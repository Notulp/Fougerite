### Class
`Fougerite.Web`

### Description
The `Web` class provides simple HTTP helpers to call external APIs/web services from a plugin, both
synchronously and asynchronously. Source: [`Web.cs`](https://github.com/Notulp/Fougerite/blob/master/Fougerite/Fougerite/Web.cs#L151)

### Methods
- `Web.GetInstance()` - Returns the singleton `Web` instance.
- `string GET(string url)` - Performs a synchronous GET request and returns the response body.
- `string GETWithSSL(string url)` - Same as `GET`, but forces SSL/TLS.
- `string POST(string url, string data, string contentType = "application/x-www-form-urlencoded")` -
  Performs a synchronous POST request.
- `string POSTWithSSL(string url, string data)` - Same as `POST`, but forces SSL/TLS.
- `void CreateAsyncHTTPRequest(string url, Action<int, string> callback, string method = "GET", string data = null, ..., string contentType = null)` -
  Performs an asynchronous HTTP request. The callback receives the response code and response body (or
  `"Failed"` on error). The callback runs on a background thread - use [`Loom`](Loom.md) if you need to
  touch the main thread/game objects from within it.
- `void DoWithResponse(HttpWebRequest request, Action<HttpWebResponse> responseAction)` - Lower-level helper
  for custom `HttpWebRequest` objects.

> Note: Since the async callback runs off the main thread, results/logs from it will typically only be
> visible in the log file, not the live console.

### Examples

#### C#
```csharp
public class MyClass
{
    [Newtonsoft.Json.JsonProperty]
    public string Something { get; set; }

    [Newtonsoft.Json.JsonProperty]
    public int Something2 { get; set; }
}

private void MyCallback(int responseCode, string response)
{
    if (response != "Failed")
    {
        Logger.Log("[TestPlugin]: Response: " + response);
    }
}

public void Test()
{
    MyClass something = new MyClass
    {
        Something = "something",
        Something2 = 666
    };

    // You will only see the result in the log file, not the console!
    Web.GetInstance().CreateAsyncHTTPRequest("url", MyCallback, "POST",
        Newtonsoft.Json.JsonConvert.SerializeObject(something),
        null, "application/json");
}
```

#### Python
```python
import json
import clr
clr.AddReferenceByPartialName("Fougerite")
clr.AddReferenceByPartialName('System.Core')
import System
from System import Action
import Fougerite


class TestPlugin:

    def webCallback(self, code, response):
        if response != "Failed":
            Fougerite.Logger.Log('[TestPlugin]: Response: ' + response)

    def test(self):
        # You will only see the result in the log file, not in console!
        MyExtraArguments = Plugin.CreateStringDict()
        Web.CreateAsyncHTTPRequest('url', Action[int, str](self.webCallback), 'POST',
            json.dumps({'name': 'test'}), MyExtraArguments, 'application/json')

    def On_PluginInit(self):
        self.test()
```

#### JavaScript
```javascript
function webCallback(code, response)
{
    if (response !== "Failed")
    {
        Server.Log("[TestPlugin]: Response: " + response);
    }
}

function test()
{
    // You will only see the result in the log file, not in console!
    Web.CreateAsyncHTTPRequest("url", webCallback, "POST",
        JSON.stringify({ name: "test" }), null, "application/json");
}

function On_PluginInit()
{
    test();
}
```

#### Lua
```lua
function webCallback(code, response)
    if response ~= "Failed" then
        Server.Log("[TestPlugin]: Response: " .. response)
    end
end

function test()
    -- You will only see the result in the log file, not in console!
    Web.CreateAsyncHTTPRequest("url", webCallback, "POST", "{\"name\":\"test\"}", nil, "application/json")
end

function On_PluginInit()
    test()
end
```
