using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EntityTestApi.Data;
using EntityTestApi.Models;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;



[ApiController]
public class ErrorController : ControllerBase
{
    [Route("error")]
    public IActionResult Error() => Problem();
}