using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]   // anonimus sme da vidi
public class OsobljeController : Controller
{
    public IActionResult Index() => View();
}