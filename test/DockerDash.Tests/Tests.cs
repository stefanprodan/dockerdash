using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace DockerDash.Tests
{
    public class Tests : IClassFixture<DockerDashFixture<Startup>>
    {
        public HttpClient Client { get; }

        public Tests(DockerDashFixture<Startup> fixture)
        {
            Client = fixture.Client;
        }

        [Fact]
        public async Task JwtLogin()
        {
            // Arrange
            int responseStatusCode = 0;
            var content = string.Empty;
            var user = "admin";
            var password = "changeme";
            var path = "token";
            var keyword = "172800";

            // Act    
            for (int i = 0; i < 4; i++)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, path);
                var requestContent = string.Format("username={0}&password={1}", Uri.EscapeDataString(user), Uri.EscapeDataString(password));
                request.Content = new StringContent(requestContent, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = await Client.SendAsync(request);
                responseStatusCode = (int)response.StatusCode;
                content = await response.Content.ReadAsStringAsync();
            }

            // Assert
            Assert.Equal(200, responseStatusCode);
            Assert.Contains(keyword, content);
        }
    }
}
