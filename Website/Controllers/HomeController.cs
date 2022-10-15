using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using WebServer.Models;

namespace Website.Controllers
{
    public class HomeController : Controller
    {
        
        public IActionResult Index()
        {
            ViewBag.Title = "Home";

            // get list of all clients
            RestClient client = new RestClient("http://localhost:50968/");
            RestRequest request = new RestRequest("api/clients", Method.Get);
            RestResponse response = client.Execute(request);

            // deserialize into list
            List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(response.Content);

            return View(clients);
        }
    }
}
