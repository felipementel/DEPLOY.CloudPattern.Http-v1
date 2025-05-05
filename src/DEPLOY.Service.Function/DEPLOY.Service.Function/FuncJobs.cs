using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DEPLOY.Service.Function
{
    public class FuncJobs
    {
        private readonly ILogger<FuncJobs> _logger;

        public FuncJobs(ILogger<FuncJobs> logger)
        {
            _logger = logger;
        }

        [Function("ListJobs")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var faker = new Faker<Jobs>(locale: "pt_BR")
                .RuleFor(j => j.Id, f => f.IndexFaker + 1)
                .RuleFor(j => j.Name, f => f.Company.CompanyName())
                .RuleFor(j => j.Description, f => f.Lorem.Sentence())
                .RuleFor(j => j.Status, f => f.PickRandom(new[] { "Pending", "In Progress", "Completed" }))
                .RuleFor(j => j.CreatedAt, f => f.Date.Past(1))
                .RuleFor(j => j.UpdatedAt, f => f.Date.Recent(1))
                .Generate(count: 10);

            return new OkObjectResult(faker);
        }
    }

    public class Jobs
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
