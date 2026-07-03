using BlogMVC.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

public class BaseApiController : ControllerBase
{
    protected bool IsValidObjectId(string id) => MongoDbHelper.IsValidObjectId(id);
}