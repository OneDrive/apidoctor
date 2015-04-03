# API Documentation Test Tool

The API documentation test tool makes it easy to validate Markdown-based API
documentation matches a REST service implementation.

The toolset includes a command line and GUI application that can be used to
perform for the following validations:

* Check for broken links in the documentation.
* Print resource and method definitions.
* Verify that the documentation is internally consistent:
  * Check that defined resources and APIs that return these resources are matched.
  * Check that example API responses are consistent with the resources they should return.
* Verify that a target REST service matches the API documentation:
  * Check that requests and responses in the documentation match the service.
  * Inject parameters into the API calls to the service.
* Publish documentation to an output folder.

## Building
To build the project, invoke either `msbuild` or `xbuild` depending on your
platform. This tool is compatible with Mono or .NET.

## Command Line Tool

![Screen shot of the command line tool in action](example-console.png)


`apidocs.exe [command] [options]`

Available commands are:

* `print` - Print files, resources, and methods discovered in the documentation.
* `check-links` - Verify links in the documentation aren't broken.
* `check-docs` - Check for errors in the documentation's resources, requests, and response examples.
* `check-service` - Check for differences between the documentation and service responses to documented requests.
* `publish` - Publish the documentation into one of the supported output formats.
* `set` - Set default parameter values for the tool

All commands (except `set`) have the following options available:

| Option             | Description                                                                                            |
|:-------------------|:-------------------------------------------------------------------------------------------------------|
| `--path <path>`    | Required. Path to the root of the documentation set to scan. Can be defaulted using the `set` command. |
| `--short`          | Print concise output to the console.                                                                   |
| `--verbose`        | Print verbose output to the console, including full HTTP requests/responses.                           |
| `--log <log_file>` | Log console output to a file.                                                                          |

### Print Command
Print information about the source files, resources, methods, and requests
that were parsed by the tool.

| Option        | Description                                                       |
|:--------------|:------------------------------------------------------------------|
| `--files`     | Output information about the files contained in the document set. |
| `--resources` | Output resource definitions read from the documentation.          |
| `--methods`   | Output method definitions read from the documentation.            |

One of these three arguments is required to use the `print` command.

### Check-links Command
Check for broken links in the documentation.

No specific options are required. Using `--verbose` will include warnings about
links that were not verified.

Example: `apidocs.exe links --path ~/github/api-docs --method search`

### Check-docs Command
The `check-docs` command ensures that the documentation is internally consistent.
It verifies that JSON examples are proper JSON, that API methods that accept or
return a specific resource type have valid request/response examples, and that
the metadata in the documentation is formatted properly.

| Option                   | Description                                                                                        |
|:-------------------------|:---------------------------------------------------------------------------------------------------|
| `--method <method_name>` | Optional. Specify the name of a request method to evaluate. If missing, all methods are evaluated. |

Example: `apidocs.exe check-docs --path ~/github/api-docs --method search`

### Check-service Command
Check the documented requests and responses against an actual REST service. Requires
the URL for the service and an OAuth access token to call the service.

| Option                             | Description                                                                                                                                                              |
|:-----------------------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `--access-token "token"`           | OAuth access token to use when calling the service. You can use set to provide a default value. You may need to escape the token value by enclosing it in double quotes. |
| `--url <url>`                      | Set the base URL for the service calls.                                                                                                                                  |
| `--parameter-file <relative_path>` | Relative path to a JSON file describing the parameter replacements to make in the request methods.                                                                       |
| `--pause`                          | Pause for a key press between API calls to the service to enable reading the responses.                                                                                  |
| `--method <method_name>`           | Check a single request/response method instead of everything in the documentation.                                                                                       |

Example:
```
apidocs set --access-token "asdkljasdkj..." -url https://api.example.org/v1.0
apidocs check-service --parameter-file requests.json --method search
```

#### Refresh Tokens
Instead of using an access token on the command line, you can use the following
environment variables to provide a refresh token and token service to generate
access tokens. This enables the tool to be used in automation scripts and other
scenarios where it may not be possible to have user-interaction to generate an
access token.

| Variable Name           | Description                                                                |
|:------------------------|:---------------------------------------------------------------------------|
| **oauth-token-service** | URL for the OAuth 2.0 token service to be used to retrieve an access token |
| **oauth-client-id**     | Client ID that is passed to the token service                              |
| **oauth-client-secret** | Client Secret that is passed to the token service                          |
| **oauth-redirect-uri**  | Redirect URI used to generate the refresh token                            |
| **oauth-refresh-token** | Refresh token that is used to generate an access token                     |

If these environment variables are set, it is not necessary to pass an access
token using the `--access-token` command line parameter. The tool will call the
token service to retrieve an access token when necessary.

### Publish Command

The publish command uses the documentation to generate a new set of outputs.

| Option               | Description                                                         |
|:---------------------|:--------------------------------------------------------------------|
| `--output <path>`    | Required. Output directory for documentation.                       |
| `--format <format>`  | Specify the format for the output documentation.                    |
| `--template <value>` | Specify the path to a folder that contains output template content. |

Example: `apidocs --path ~/github/api-docs --output ~/documents/docs`

#### Publish Formats

The following formats are supported:

| Value    | Description                                                                                                                     |
|:---------|:--------------------------------------------------------------------------------------------------------------------------------|
| markdown | Creates a copy of the documentation in markdown format.                                                                         |
| html     | Generates a simple HTML output with a default style/format.                                                                     |
| swagger2 | Experimental: Generates a swagger 2 compatible output file from the documentation.                                              |
| mustache | Use a mustache template language to generate html output. Requires a --template <path> and a template.htm file inside that path |

#### Swagger2 Options

The following additional command line options are required for swagger2 output:
| Name                    | Description                                                         |
|:------------------------|:--------------------------------------------------------------------|
| **swagger-title**       | Title of the API in the Swagger header .                            |
| **swagger-description** | Description of the API in the Swagger header.                       |
| **swagger-version**     | Version number (1.0) in the Swagger header.                         |
| **swagger-auth-scope**  | Set the required auth scope for every method in the Swagger output. |


### Set Command
The set command lets you preset values for some parameters so they don't need to
be specified on each command line. These values are stored in the app.config
file next to the application executable.

Example: `apidocs set --path ~/github/api-docs --url https://api.example.org/v1.0`

| Option                   | Description                                        |
|:-------------------------|:---------------------------------------------------|
| `--path <path>`          | Set the path to the documentation folder.          |
| `--access-token "token"` | Set the access token used for calling the service. |
| `--url <url>`            | Set the base URL for API calls.                    |
| `--reset`                | Erase any stored values.                           |
| `--print`                | Print any currently stored values.                 |

## Documentation Format
To work with this test tool, the source documentation has a few basic requirements:

* Documentation must be in plain text or markdown format.
* Requests and responses in the documentation are full HTTP style examples.
* Requests, responses, and resources are enclosed in fenced code blocks (three backticks ` ``` `).
* Requests, responses, and resources have simple metadata enclosed in an HTML comment immediately before the codeblock.

### Code-block Metadata
To enable the tool to categorize and process the code blocks correctly a simple
JSON metadata block is required for each code block. These are enclosed as an HTML
comment block so as to not be visible in the rendered markdown.

This metadata block is deserialized into a `CodeBlockAnnotation` in the tool,
which includes the following definition:

```
{
  "blockType": "resource | request | response | ignored",
  "@odata.type": "resource_identifier",
  "optionalProperties": ["array", "of", "properties", "considered", "optional"],
  "isCollection": "bool value to treat the response as a collection of a resource type",
  "truncated": "bool value that the response may be missing properties required by the resource",
  "name": "name of the request method"
}
```

An example usage would look like this in the markdown:

```
### Resource Definition

<!-- {"blockType": "resource", "@odata.type": "example_item"} -->
\```
{
  "id": "string",
  "name": "string",
  "count": 123
}
\```

### Example Request

<!-- {"blockType": "request", "name": "example"} -->
\```
GET /drive/items/root
Accept: application/json
\```

### Response

<!-- {"blockType": "response", "@odata.type": "example_item", "truncated": true} -->
\```
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "!23134!",
  "name": "root_folder",
  "count": 4
}
\```
```

This file, if included in the documentation, would be read as one resource,
`example_item` that has a JSON object schema with three properties: `string id`,
`string name` and `number count`. It then has one request method which calls
`GET <url_root>/drive/items/root` and returns an `example_item` resource.

Using `check-docs` would verify that the return example in the documentation
matches the proper schema. Using `check-service` would verify that the service
responses to the request following the schema.

Request / response pairs are identified by matching up the next response found
after the request codeblock. You can have other codeblocks between a request and
a response as long as they are missing the metadata or tagged as
`"blockType": "ignored"`. A given file can have as many resources and
request/response pairs as necessary.

## Request Parameters

The tool also supports defining parameters for requests in a separate file. This
information is loaded and used to make one or more requests to the service by
substituting values for placeholders in the initial request.

For example, in a request for an item with a particular ID, you might write the
request to look like this:

```
<!-- { "blockType": "request"; "name": "get-drive" } -->
GET /drives/{drive-id}
```

However, when the test tool makes the API call to the service, calling it verbatim
would result in an error. Request parameters allow you to define one or more
scenarios that are used to call the method.

A scenario can have one or more statically defined properties. It can also include
an HTTP request and substitute one or more placeholder values with data from
the response to that request.

The scenario file contains a single JSON array, with each member of the array
conforming to this schema:

```json
{
  "name": "Copy test_copy_file to a new location",
  "method": "copy-item",
  "enabled": true,
  "test-setup": [
    {
      "method": "upload-via-put",
      "http-request": "PUT /drive/root:/test_copy_file.txt:/content\r\nContent-Type: application/octet-stream\r\n\r\nTest file that we will copy to another location",
      "request-parameters":
      {
        "{path-to-file}": "/test_copy.file.txt",
        "$body": "Test file that we will copy to another location",
        "Content-Type:": "application/octet-stream"
      },
      "allowed-status-codes": [ 200 ],
      "capture": {
         "[source-file-id]": "$.id",
         "[response-type]": "Content-Type:",
         "[response-body]": "!body"
         }
    }
  ],
  "request-parameters":
  {
    "{item-id}": "[source-file-id]"
  }
}
```

| Property             | Type            | Description                                                                                                                                     |
|:---------------------|:----------------|:------------------------------------------------------------------------------------------------------------------------------------------------|
| `name`               | string          | The name of the scenario described.                                                                                                             |
| `method`             | string          | The name of the method this scenario uses. Either defined in the documentation or a substitute name is auto-generated.                          |
| `enabled`            | bool            | Enable or disable the scenario.                                                                                                                 |
| `test-setup`         | array           | See below.                                                                                                                                      |
| `request-parameters` | key-value pairs | Specify the key-value pairs for parameters for the request. The key is used as a placeholder name, and the value is subed into the placeholder. |


### Test Setup

The test-setup property allows you to define an array of calls that are made
before the actual test method is executed. This allows you to pull values from
other requests and store them to be used in the test method call. This also
allows you to chain together multiple calls from the documentation to enable
testing complex scenarios, like fragment uploads.

Each object in the array of `test-setup` is a `PlaceholderRequest` instance.

| Property               | Type            | Description                                                                                                                                                                                                   |
|:-----------------------|:----------------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `method`               | string          | The name of a method from the documentation that should be used as this test-setup call                                                                                                                       |
| `http-request`         | string          | Instead of specifying a method from the docs, you can input a raw HTTP request to be used.                                                                                                                    |
| `request-parameters`   | key-value pairs | Specify the key-value pairs for parameters for the request. The key is used as a placeholder name, and the value is subed into the placeholder.                                                               |
| `allowed-status-codes` | array of int    | Normally the request is considered failed of the response is anything other than 2xx. Use this to allow error codes and other responses to be considered valid.                                               |
| `capture`              | key-value pairs | Specify the key-value pairs of values that are read from this response and stored for another request under this scenario. Allows you to store values and use them in other requests under the same scenario. |

### Placeholder Grammar

When specifying a placeholder name or value, the following syntax is used:

| Syntax        | Example            | Description                                                                                                                               |
|:--------------|:-------------------|:------------------------------------------------------------------------------------------------------------------------------------------|
| Curly Braces  | `{path-to-file}`   | Find and update a value in the URL matching the full string.                                                                              |
| Square Braces | `[source-file-id]` | Look for a previous stored value that was output from a previous request within the same scenario.                                        |
| JPath         | `$.id`             | Replace a property value in the JSON body of the request. If the content-type of the request is not application/json an error will occur. |
| !body         | `!body`            | Replace the content stream of the request with the provided value                                                                         |
| !url          | `!url`             | Replace the URL for the request with the provided value.                                                                                  |
| Header:       | `Content-Type:`    | Replace the value of a header with the specified value. Note the header name must end with a colon to be valid.                           |

### Capture Grammar

The `key` of anything in the `capture` node MUST be wrapped in square
brackets `[foobar]`. Otherwise the parameters will not be considered value.

The output-value grammar follows the same syntax as the placeholder grammar:

| Syntax  | Example         | Description                                           |
|:--------|:----------------|:------------------------------------------------------|
| JPath   | $.id            | Read and store the value at the JPath                 |
| Header: | `Content-Type:` | Read and store the value of the specified HTTP header |
| !body   | !body           | Read and store the complete body of the response      |

### Code Block Annotation Properties

The HTML-comment enclosed JSON object inside the documentation has the following
properties defined:

```json
{
	"blockType": "unknown | resource | request | response | ignored | example | simulatedResponse",
	"@odata.type": "resource name",
	"optionalProperties": [ "prop1", "prop2" ],
	"isCollection": false,
	"collectionProperty": "value",
	"isEmpty": false,
	"truncated": true,
	"name": "string name",
	"expectError": false,
	"nullableProperties": [ "prop3", "prop4" ]
}
```

#### Property Descriptions

| Name                   | Value            | Allowed Blocks                       | Description                                                                                                                                                                                                     |
|:-----------------------|:-----------------|:-------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **blockType**          | string           | All                                  | Describes the type of the json block proceeding the annotation.                                                                                                                                                 |
| **@odata.type**        | string           | All                                  | Describes the name of the resource (either being defined, in the case of a resource block, or as the body type on a request/response block)                                                                     |
| **optionalProperties** | array of strings | resource                             | An array of properties that are not required to be in the code block.                                                                                                                                           |
| **isCollection**       | boolean          | response, example, simulatedResponse | Indicates that the block contains a collection of items that match the **@odata.type** schema. This is expected as an object with a single property that is an array of objects.                                |
| **collectionProperty** | string           | response, example, simulatedResponse | Provides the name of the variable that contains the collection. Default value: `value`.                                                                                                                         |
| **isEmpty**            | boolean          | response, example, simulatedResponse | Indicates that the collection value is expected to be empty (or not).                                                                                                                                           |
| **truncated**          | boolean          | response, example, simulatedResponse | Indicates that the block will not include all properties of the resource and that's not an error. Properties explicitly shown in the code block are always considered required when tested against the service. |
| **name**               | string           | request, example                     | Provides the name of the request method being defined.                                                                                                                                                          |
| **expectError**        | boolean          | response, example, simulatedResponse | Use this to indicate that instead of returning the normal response as defined, an error response will be returned instead.                                                                                      |
| **nullableProperties** | array of strings | response, example, simulatedResponse | Provide a list of properties that are allowed to have null values. By default, null values for a property will generate a warning.                                                                              |


#### Block Types
| Name                | Description                                                                                                                          |
|:--------------------|:-------------------------------------------------------------------------------------------------------------------------------------|
| `resource`          | The json block describes a system resource (complex type) in the API.                                                                |
| `request`           | The json block describes an HTTP request that can be made by clients.                                                                |
| `response`          | The json block describes the HTTP response that is sent from the service.                                                            |
| `example`           | An example of the JSON data that would be generated by the client or returned by the service, without being wrapped in an HTTP call. |
| `simulatedResponse` | Used for unit testing to simulate responses from the service.                                                                        |
| `ignored`           | No processing is done on the code block that follows.                                                                                |


## Open Source
The API Documentation Test Tool uses the following open source components:

* [MarkdownDeep](https://github.com/toptensoftware/MarkdownDeep) - Markdown for C# parser. Apache 2.0 license, Copyright (C) 2010-2011 Topten Software.
* [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) - Json parser for .NET apps. MIT license, Copyright (c) 2007 James Newton-King
* [CommandLineParser](https://commandline.codeplex.com/) - Command line parser library. MIT license, Copyright (c) 2005 - 2012 Giacomo Stelluti Scala.
* [AsyncEx](https://github.com/StephenCleary/AsyncEx/) - Command line Async helper. MIT license, Copyright (c) 2014 StephenCleary.
* [mustache-sharp] (https://github.com/jehugaleahsa/mustache-sharp) - An extension of the mustache text template engine for .NET. Public domain.
