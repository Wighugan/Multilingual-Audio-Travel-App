using System;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.CSharp;

namespace VinhKhanhLoadTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var httpClient = new HttpClient(handler);

            string baseUrl = "http://localhost:5068";

            var scenario = Scenario.Create("test_delay_lay_thong_tin", async context =>
            {
                try
                {
                    var response = await httpClient.GetAsync($"{baseUrl}/api/pois");

                    string statusCodeStr = ((int)response.StatusCode).ToString();

                    if (response.IsSuccessStatusCode)
                    {
                        return Response.Ok(statusCode: statusCodeStr);
                    }
                    else
                    {
                        Console.WriteLine($"[SERVER BÁO LỖI]: Mã {statusCodeStr} - Dữ liệu trả về: {await response.Content.ReadAsStringAsync()}");
                        return Response.Fail(statusCode: statusCodeStr);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LỖI SẬP NGUỒN]: {ex.Message}");
                    return Response.Fail(statusCode: "500_Server_Exception");
                }
            })
            .WithLoadSimulations(
                Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            //thay rate thanh so nguoi muon mo phong, interval la khoang thoi gian giua cac lan mo phong, during la tong thoi gian chay load test
            );

            NBomberRunner.RegisterScenarios(scenario)
                         .WithReportFileName("Delay_Fetch_Data_Report")
                         .Run();
        }
    }
}