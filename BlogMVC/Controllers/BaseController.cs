using BlogMVC.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

public class BaseController : Controller
{
    protected bool IsValidObjectId(string id) => MongoDbHelper.IsValidObjectId(id);
}