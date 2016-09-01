using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DockerDash
{
    public class DockerService
    {
        private Task monitorContainersTask;
        private DockerClient docker;
        private readonly IConnectionManager _signalManager;

        public string Host { get; set; }
        public DockerService(IOptions<DockerHost> options, IConnectionManager signalManager)
        {
            Host = options.Value.Uri;
            _signalManager = signalManager;
            docker = new DockerClientConfiguration(new Uri(Host)).CreateClient();
        }

        public List<ContainerModel> GetContainerList()
        {
            var containers = docker.Containers.ListContainersAsync(new ContainersListParameters()
            {
                All = true
            }).Result;

            return containers.Select(c =>
            {
                var cont = new ContainerModel
                {
                    Id = c.ID,
                    Name = c.Names.First().StartsWith("/") ? c.Names.First().Remove(0,1) : c.Names.First(),
                    Image = c.Image,
                    State = c.State,
                    Status = c.Status,
                    Command = c.Command,
                    Created = c.Created.ToString("dd-MM-yy HH:mm")
                };

                if(cont.State.ToLowerInvariant() == "running")
                {
                    //var stats = GetContainerStats(c.ID);
                    //cont.MemoryUsage = FormatBytes(stats.MemoryStats.Usage);
                    cont.IpAddress = c.NetworkSettings.Networks.Select(n => n.Value.IPAddress).Aggregate((current, next) => current + ", " + next);
                    cont.Ports = c.Ports.Select(n => n.PrivatePort.ToString() + "/" + n.Type).Aggregate((current, next) => current + ", " + next);
                }

                return cont;

            }).ToList();
        }

        public List<ImageModel> GetImageList()
        {
            var images = docker.Images.ListImagesAsync(new ImagesListParameters()
            {
                All = true
            }).Result;

            if(images == null || !images.Any())
            {
                return new List<ImageModel>();
            }

            return images.Select(c =>
            {
                var img = new ImageModel
                {
                    Id = c.ID,
                    Size = FormatBytes((ulong)c.Size),
                    ParentID = c.ParentID.Replace("sha256:", ""),
                    VirtualSize = FormatBytes((ulong)c.VirtualSize),
                    RepoDigests = c.RepoDigests,
                    RepoTags = (c.RepoTags != null && c.RepoTags.Any()) ? c.RepoTags.Aggregate((current, next) => current + ", " + next) : null,
                    Labels = (c.Labels != null && c.Labels.Any()) ? c.Labels.Keys.Aggregate((current, next) => current + ", " + next) : null,
                    Created = c.Created.ToString("dd-MM-yy HH:mm")
                };

                if(c.RepoTags != null && c.RepoTags.Any())
                {
                    if(c.RepoTags.First().Contains("<none>:<none>"))
                    {
                        img.Name = c.ID.Replace("sha256:", "");
                        if(img.Name.Length > 12)
                        {
                            img.Name = img.Name.Substring(0, 12);
                        }
                        img.RepoTags = "untagged";
                    }
                    else
                    {
                        img.Name = c.RepoTags.First().Split(':').First();
                    }
                }
                else
                {
                    img.Name = c.ID.Replace("sha256:", "");
                }

                return img;

            }).ToList();
        }

        public dynamic GetMemoryStats(string id)
        {
            var inspec = docker.Containers.InspectContainerAsync(id).Result;
            if (inspec.State.Running)
            {
                var stats = GetContainerStats(id);
                var rxTotal = Convert.ToUInt64(stats.Networks.Values.Sum(n => Convert.ToDecimal(n.RxBytes)));
                var txTotal = Convert.ToUInt64(stats.Networks.Values.Sum(n => Convert.ToDecimal(n.TxBytes)));

                ulong ioReadTotal = 0;
                ulong ioWriteTotal = 0;

                if(stats.BlkioStats.IoServiceBytesRecursive.Any())
                {
                    ioReadTotal = stats.BlkioStats.IoServiceBytesRecursive[0].Value;
                    ioWriteTotal = stats.BlkioStats.IoServiceBytesRecursive[1].Value;
                }

                return new
                {
                    memory = new
                    {
                        value = ConvertBytesToKilo(stats.MemoryStats.Usage),
                        label = FormatBytes(stats.MemoryStats.Usage)
                    },
                    network = new
                    {
                        valuerx = ConvertBytesToKilo(rxTotal),
                        labelrx = FormatBytes(rxTotal),
                        valuetx = ConvertBytesToKilo(txTotal),
                        labeltx = FormatBytes(txTotal)
                    },
                    io = new
                    {
                        valuerx = ConvertBytesToKilo(ioReadTotal),
                        labelrx = FormatBytes(ioReadTotal),
                        valuetx = ConvertBytesToKilo(ioWriteTotal),
                        labeltx = FormatBytes(ioWriteTotal)
                    },
                    pids = stats.PidsStats.Current,
                    cpuTime = TimeSpan.FromTicks(Convert.ToInt64(stats.CPUStats.CPUUsage.TotalUsage/100)).ToString("c")
                };
            }

            return new
            {
                memory = new
                {
                    value = 0,
                    label = "0 MB"
                },
                network = new
                {
                    valuerx = 0,
                    labelrx = "0 KB",
                    valuetx = 0,
                    labeltx = "0 KB"
                }
            };
        }

        static double ConvertBytesToKilo(ulong bytes)
        {
            return (bytes / 1024f);
        }

        public void MonitorEvents()
        {
            if (monitorContainersTask == null || monitorContainersTask.Status != TaskStatus.Running)
            {
                monitorContainersTask = Task.Factory.StartNew(() =>
                {
                    var hubContext = _signalManager.GetHubContext<MainHub>();
                    using (var stream = docker.Miscellaneous.MonitorEventsAsync(new ContainerEventsParameters(), CancellationToken.None).Result)
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            while (stream.CanRead)
                            {
                                var eventString = sr.ReadLine();
                                var obj = JsonConvert.DeserializeObject<dynamic>(eventString);
                                if (obj.Type == "container")
                                {
                                    hubContext.Clients.All.OnContainerEvent(eventString);
                                }

                            }
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        public ContainerStatsResponse GetContainerStats(string id)
        {
            string stats;
            using (var stream = docker.Containers.GetContainerStatsAsync(id, new ContainerStatsParameters() { Stream = false }, CancellationToken.None).Result)
            {
                using (var sr = new StreamReader(stream))
                {
                    stats = sr.ReadLine();
                }
            }

            return JsonConvert.DeserializeObject<ContainerStatsResponse>(stats);
        }

        public string GetContainerLogs(string id, int tail)
        {
            var logs = new StringBuilder();
            using (var stream = docker.Containers.GetContainerLogsAsync(id, new ContainerLogsParameters()
            {
                Timestamps = false,
                Follow = false,
                ShowStderr = true,
                ShowStdout = true,
                Tail = tail.ToString()
            }, new CancellationTokenSource(5000).Token).Result)
            {
                using (var sr = new StreamReader(stream))
                {
                    string s = String.Empty;
                    while ((s = sr.ReadLine()) != null)
                    {
                        logs.AppendLine(s);
                    }

                }
            };

            return logs.ToString();
        }

        public static string FormatBytes(ulong input)
        {
            long bytes = Convert.ToInt64(input);
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            bool isNegative = bytes < 0;
            if (isNegative) bytes = (-1) * bytes;
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
                dblSByte = bytes / 1024.0;
            return String.Format("{0:0.##} {1}", isNegative ? (-1) * dblSByte : dblSByte, Suffix[i]);
        }

        public ContainerDetailsModel GetContainerDetails(string id)
        {
            var inspec = docker.Containers.InspectContainerAsync(id).Result;

            var details = new ContainerDetailsModel
            {
                Id = inspec.ID,
                State = inspec.State.Status,
                Name = inspec.Name,
                Image = inspec.Config.Image,
                Created = inspec.Created.ToString("dd-MM-yy HH:mm"),
                Driver = inspec.Driver,
                RestartCount = inspec.RestartCount,
                StartedAt = Convert.ToDateTime(inspec.State.StartedAt).ToString("dd-MM-yy HH:mm"),
                Command = inspec.Args.Aggregate((current, next) => current + " " + next)
            };

            if (inspec.State.Running)
            {
                if (inspec.NetworkSettings != null && inspec.NetworkSettings.Networks != null && inspec.NetworkSettings.Networks.Any())
                {
                    details.IpAddress = inspec.NetworkSettings.Networks.Select(n => n.Value.IPAddress).Aggregate((current, next) => current + ", " + next);
                    var portData = string.Empty;

                    if (inspec.NetworkSettings.Ports != null)
                    {
                        foreach (var port in inspec.NetworkSettings.Ports)
                        {
                            if (port.Value != null)
                            {
                                foreach (var item in port.Value)
                                {
                                    portData += $" {item.HostIP}:{item.HostPort} ->";
                                }
                            }

                            portData += $"{port.Key} ";
                        }
                    }
                    details.Ports = portData;
                }
            }

            return details;
        }

        public HostModel GetHostInfo()
        {
            var info = docker.Miscellaneous.GetSystemInfoAsync().Result;
            return new HostModel
            {
                Architecture = info.Architecture,
                Containers = info.Containers,
                ContainersPaused = info.ContainersPaused,
                ContainersRunning = info.ContainersRunning,
                ContainersStopped = info.ContainersStopped,
                DefaultRuntime = info.DefaultRuntime,
                Driver = info.Driver,
                Id = info.ID,
                Images = info.Images,
                KernelVersion = info.KernelVersion,
                LoggingDriver = info.LoggingDriver,
                MemTotal = FormatBytes((ulong)info.MemTotal),
                NCPU = info.NCPU,
                Name = info.Name,
                OperatingSystem = info.OperatingSystem,
                OSType = info.OSType,
                ServerVersion = info.ServerVersion,
                ExecutionDriver = info.ExecutionDriver,
                CgroupDriver = info.CgroupDriver,
                NGoroutines = info.NGoroutines,
                SwarmMode = info.Swarm.LocalNodeState,
                SwarmManagers = info.Swarm.Managers,
                SwarmNodes = info.Swarm.Nodes
            };
        }
    }
}
