using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace UnhandledMiddlewareExceptions.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private WeatherForecast _forecast;

        public WeatherForecastController()
        {
            _forecast = new WeatherForecast
            {
                Date = DateTime.Now,
                Summary = "Sunny",
                TemperatureC = 25
            };
        }

        [HttpGet]
        [Route("crash")]
        public WeatherForecast GetCrashed()
        {
            //Intentionally not awaited to cause an issue.
            CrashMeAsyncVoid();

            return _forecast;
        }

        private async void CrashMeAsyncVoid()
        {
            //Simulate async work
            await Task.Delay(100);

            throw new Exception("Crashed by async void method.");
        }

        [HttpGet]
        [Route("uncaught")]
        public WeatherForecast GetUncaughtException()
        {
            //Intentionally not awaited to cause an issue.
            CrashMeAsyncTask();

            return _forecast;
        }

        private async Task CrashMeAsyncTask()
        {
            //Simulate async work
            await Task.Delay(100);

            throw new Exception("Crashed by async Task method.");
        }
    }
}
