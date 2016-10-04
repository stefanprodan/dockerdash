using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DockerDash
{
    public class DockerService
    {
        private Task monitorContainersTask;
        private DockerClient docker;
        private readonly IConnectionManager _signalManager;
        private readonly ILogger<DockerService> _logger;

        public string Host { get; set; }
        public DockerService(IOptions<DockerHost> options, IConnectionManager signalManager, ILogger<DockerService> logger)
        {
            _logger = logger;
            Host = options.Value.Uri;
            _signalManager = signalManager;
            docker = new DockerClientConfiguration(new Uri(Host)).CreateClient();
        }

        public async Task<HostModel> GetHostInfo()
        {
            try
            {
                var info = await docker.Miscellaneous.GetSystemInfoAsync();
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
            catch (Exception ex)
            {
                _logger.LogError(1001, ex, ex.Message);
                throw;
            }
        }

        public async Task<List<ContainerModel>> GetContainerList()
        {
            try
            {
                var containers = await docker.Containers.ListContainersAsync(new ContainersListParameters()
                {
                    All = true
                });

                return containers.Select(c =>
                {
                    var cont = new ContainerModel
                    {
                        Id = c.ID,
                        Name = c.Names.First().StartsWith("/") ? c.Names.First().Remove(0, 1) : c.Names.First(),
                        Image = c.Image,
                        State = c.State,
                        Status = c.Status,
                        Command = c.Command,
                        Created = c.Created.ToString("dd-MM-yy HH:mm")
                    };

                    if (cont.State.ToLowerInvariant() == "running")
                    {
                        //var stats = GetContainerStats(c.ID);
                        //cont.MemoryUsage = FormatBytes(stats.MemoryStats.Usage);

                        if (c.NetworkSettings != null && c.NetworkSettings.Networks.Any())
                        {
                            cont.IpAddress = c.NetworkSettings.Networks.Select(n => n.Value.IPAddress).Aggregate((current, next) => current + ", " + next);
                        }

                        if (c.Ports.Any())
                        {
                            cont.Ports = c.Ports.Select(n => n.PrivatePort.ToString() + "/" + n.Type).Aggregate((current, next) => current + ", " + next);
                        }
                    }

                    return cont;

                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(1001, ex, ex.Message);
                throw;
            }
        }

        public async Task<List<NetworkModel>> GetNetworkList()
        {
            try
            {
                var networks = await docker.Networks.ListNetworksAsync();
                return networks.Select(n =>
                {
                    var net = new NetworkModel
                    {
                        Id = n.ID,
                        Containers = (n.Containers != null && n.Containers.Any()) ? n.Containers.Count() : 0,
                        Driver = n.Driver,
                        Name = n.Name,
                        Scope = n.Scope
                    };

                    return net;
                }).OrderBy(n => n.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(1001, ex, ex.Message);
                throw;
            }

        }

        public async Task<List<ImageModel>> GetImageList()
        {
            try
            {
                var images = await docker.Images.ListImagesAsync(new ImagesListParameters()
                {
                    All = false
                });

                if (images == null || !images.Any())
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

                    if (c.RepoTags != null && c.RepoTags.Any())
                    {
                        if (c.RepoTags.First().Contains("<none>:<none>"))
                        {
                            img.Name = c.ID.Replace("sha256:", "");
                            if (img.Name.Length > 12)
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
            catch (Exception ex)
            {
                _logger.LogError(1001, ex, ex.Message);
                throw;
            }
        }

        public async Task<dynamic> GetContainerStats(string id)
        {
            try
            {
                var inspec = await docker.Containers.InspectContainerAsync(id);
                if (inspec.State.Running)
                {
                    var stats = await GetStats(id);
                    var rxTotal = Convert.ToUInt64(stats.Networks.Values.Sum(n => Convert.ToDecimal(n.RxBytes)));
                    var txTotal = Convert.ToUInt64(stats.Networks.Values.Sum(n => Convert.ToDecimal(n.TxBytes)));

                    ulong ioReadTotal = 0;
                    ulong ioWriteTotal = 0;

                    if (stats.BlkioStats.IoServiceBytesRecursive.Any())
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
                        cpuTime = TimeSpan.FromTicks(Convert.ToInt64(stats.CPUStats.CPUUsage.TotalUsage / 100)).ToString("c")
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
            catch (Exception ex)
            {
                _logger.LogError(1001, ex, ex.Message);
                throw;
            }
        }

        public void MonitorEvents()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(1001, ex, ex.Message);
                throw;
            }
        }

        public bool IsMonitoringEvents()
        {
            return monitorContainersTask != null && monitorContainersTask.Status == TaskStatus.Running;
        }

        public async Task<ContainerDetailsModel> GetContainerDetails(string id)
        {
            try
            {
                var inspec = await docker.Containers.InspectContainerAsync(id);

                var details = new ContainerDetailsModel
                {
                    Id = inspec.ID,
                    State = inspec.State.Status,
                    Name = inspec.Name,
                    Image = inspec.Config.Image,
                    Created = inspec.Created.ToString("dd-MM-yy HH:mm"),
                    Driver = inspec.Driver,
                    RestartCount = inspec.RestartCount,
                    Path = inspec.Path,
                    StartedAt = string.IsNullOrEmpty(inspec.State.StartedAt) ? null : Convert.ToDateTime(inspec.State.StartedAt).ToString("dd-MM-yy HH:mm")
                };

                if (inspec.Args != null && inspec.Args.Any())
                {
                    details.Command = inspec.Args.Aggregate((current, next) => current + " " + next);
                }

                if (inspec.Config != null)
                {
                    details.WorkingDir = inspec.Config.WorkingDir;

                    if (inspec.Config.Entrypoint != null && inspec.Config.Entrypoint.Any())
                    {
                        details.Entrypoint = inspec.Config.Entrypoint.ToList();
                    }

                    if (inspec.Config.Env != null && inspec.Config.Env.Any())
                    {
                        details.Env = inspec.Config.Env.ToList();
                    }
                }


                if (inspec.Mounts != null && inspec.Mounts.Any())
                {
                    details.Mounts = new List<string>();
                    foreach (var m in inspec.Mounts)
                    {
                        details.Mounts.Add($"{m.Source}:{m.Destination}");
                    }
                }

                if (inspec.State.Running)
                {
                    if (inspec.NetworkSettings != null && inspec.NetworkSettings.Networks != null && inspec.NetworkSettings.Networks.Any())
                    {
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

                        details.Networks = new List<string>();
                        foreach (var net in inspec.NetworkSettings.Networks)
                        {
                            details.Networks.Add($"{net.Key} IP: {net.Value.IPAddress} Gateway: {net.Value.Gateway} Mac: {net.Value.MacAddress}");
                        }
                    }
                }
                else
                {
                    details.FinishedAt = string.IsNullOrEmpty(inspec.State.FinishedAt) ? null : Convert.ToDateTime(inspec.State.FinishedAt).ToString("dd-MM-yy HH:mm");
                }

                return details;
            }
            catch (Exception ex)
            {
                if (ex.InnerException is DockerContainerNotFoundException)
                {
                    return null;
                }
                else
                {
                    _logger.LogError(1001, ex, ex.Message);
                    throw;
                }
            }
        }

        private async Task<ContainerStatsResponse> GetStats(string id)
        {
            try
            {
                string stats;
                using (var stream = await docker.Containers.GetContainerStatsAsync(id, new ContainerStatsParameters() { Stream = false }, CancellationToken.None))
                {
                    using (var sr = new StreamReader(stream))
                    {
                        stats = sr.ReadLine();
                    }
                }

                return JsonConvert.DeserializeObject<ContainerStatsResponse>(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(1001, ex, ex.Message);
                throw;
            }
        }

        public async Task<string> GetContainerLogs(string id, int tail)
        {
            try
            {
                var logs = new StringBuilder();
                using (var stream = await docker.Containers.GetContainerLogsAsync(id, new ContainerLogsParameters()
                {
                    Timestamps = false,
                    Follow = false,
                    ShowStderr = true,
                    ShowStdout = true,
                    Tail = tail.ToString()
                }, new CancellationTokenSource(5000).Token))
                {
                    using (var sr = new StreamReader(stream))
                    {
                        string s = String.Empty;
                        while ((s = sr.ReadLine()) != null)
                        {
                            // remove ANSI x3.64 graphic codes
                            logs.AppendLine(Regex.Replace(s, @"\e\[(\d+;)*(\d+)?[ABCDHJKfmsu]", ""));
                        }

                    }
                };

                return logs.ToString();
            }
            catch (Exception ex)
            {
                if (ex.InnerException is DockerContainerNotFoundException)
                {
                    return null;
                }
                else
                {
                    _logger.LogError(1001, ex, ex.Message);
                    throw;
                }
            }
        }

        static string FormatBytes(ulong input)
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

        static double ConvertBytesToKilo(ulong bytes)
        {
            return (bytes / 1024f);
        }

    }
}
