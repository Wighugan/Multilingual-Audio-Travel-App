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

            var scenario = Scenario.Create("test_hang_doi_queue", async context =>
            {
                try
                {
                    // 1. ĐÃ SỬA THÀNH POST VÀ GỌI ĐÚNG VÀO API ĐẾM LƯỢT (QUÁN SỐ 1)
                    var response = await httpClient.PostAsync($"{baseUrl}/api/pois/1/analytics?type=visit", null);

                    string statusCodeStr = ((int)response.StatusCode).ToString();

                    if (response.IsSuccessStatusCode)
                    {
                        return Response.Ok(statusCode: statusCodeStr);
                    }
                    else
                    {
                        Console.WriteLine($"[SERVER BÁO LỖI]: Mã {statusCodeStr}");
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
                // 2. TĂNG TẢI LÊN 500 REQUEST/GIÂY TRONG 10 GIÂY (Tổng 5000 lượt)
                Simulation.Inject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
            );

            NBomberRunner.RegisterScenarios(scenario)
                         .WithReportFileName("Queue_Load_Test_Report")
                         .Run();
        }
    }
}