using COEPulse.API.DTO;
using COEPulse.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace COEPulse.API.Controllers
{
    [ApiController]
    [Route("api/coe")]
    public class IndexController(DataService ds)
        : ControllerBase
    {
        [HttpGet]
        public COEQueryResult Get([FromQuery] Filters filters)
        {
            return ds.GetRecords(filters);
        }
    }
}
