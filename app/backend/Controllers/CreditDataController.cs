using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Threading.Tasks;

using backend.Model;
using backend.Model.CreditDataApi;
using System.Net.Http;
using System.Collections.Generic;

namespace backend.Controllers
{
    [ApiController]
    [Route("{credit-data}/{ssn}")]
    public class CreditDataController : Controller
    {
        // Tasks says only these are valid, anything else is a 404.
        private static readonly HashSet<string> _validSSNs = new()
        {
            "424-11-9327",
            "553-25-8346",
            "287-54-7823"
        };

        // Can do better on this, list of good endpoints, variable arguments, List<T> implementations etc
        private async Task<T> ApiCallAsync<T>(string endPoint, string ssn) where T : class, new()
        {
            using(HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync($"{creditDataUri}{endPoint}/{ssn}"))
                {
                    string data = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(data);
                }
            }
        }

        // Should be in config
        private static readonly string creditDataUri = "https://infra.devskills.app/api/credit-data/";

        [HttpGet]
        public async Task<CreditData> Get(string ssn)
        {
            // Real apps don't do this and would raise exceptions that get returned properly, didn't have time to
            // implement them this way.
            if (string.IsNullOrEmpty(ssn))
            {
                Response.StatusCode = 400;                
                return new CreditData();
            }

            // Invalid SSN?
            if (!_validSSNs.Contains(ssn))
            {
                Response.StatusCode = 404;
                return new CreditData();

            }

            // Line up our calls and do them together

            try
            {
                Task<PersonalDetails> personalTask = ApiCallAsync<PersonalDetails>("personal-details", ssn);
                Task<AssessedIncomeDetails> assessedTask = ApiCallAsync<AssessedIncomeDetails>("assessed-income", ssn);
                Task<DebtDetails> debtTask = ApiCallAsync<DebtDetails>("debt", ssn);
                await Task.WhenAll(personalTask, assessedTask, debtTask);

                CreditData creditData = new()
                {
                    address = personalTask.Result.address,
                    assessed_income = assessedTask.Result.assessed_income,
                    balance_of_debt = debtTask.Result.balance_of_debt,
                    complaints = debtTask.Result.complaints,
                    first_name = personalTask.Result.first_name,
                    last_name = personalTask.Result.last_name
                };
                return creditData;
            }
            catch (Exception)
            {
                Response.StatusCode = 400;
                return new CreditData();
            }

        }
    }
}
