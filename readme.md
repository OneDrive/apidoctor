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
* Publish sanitized documentation to an output folder.

## Building
To build the project, invoke either `msbuild` or `xbuild` depending on your
platform. This tool is compatible with Mono or .NET.

## Command Line Tool

`apidocs.exe [command] [options]`

Available commands are:

* `print` - Print files, resources, and methods discovered in the documentation.
* `links` - Verify links in the documentation aren't broken.
* `check-docs` - Check for errors in the documentation's resources, requests, and response examples.
* `check-service` - Check for differences between the documentation and service responses to documented requests.
* `publish` - Create a sanitized version of the documentation without internal comments
* `set` - Set default parameter values for the tool

All commands (except `set`) have the following options available:

Option | Description
---|---
`--path <path>` | Required. Path to the root of the documentation set to scan. Can be defaulted using the `set` command.
`--short` | Print concise output to the console.
`--verbose` | Print verbose output to the console, including full HTTP requests/responses.
`--log <log_file>` | Log console output to a file.

### Print Command
Print information about the source files, resources, methods, and requests
that were parsed by the tool.

Option | Description
---|---
`--files` | Output information about the files contained in the document set.
`--resources` | Output resource definitions read from the documentation.
`--methods` | Output method definitions read from the documentation.

One of these three arguments is required to use the `print` command.

### Links Command
Check for broken links in the documentation.

No specific options are required. Using `--verbose` will include warnings about
links that were not verified.

Example: `apidocs.exe links --path ~/github/onedrive-api-docs --method search`

### Check-docs Command
The `check-docs` command ensures that the documentation is internally consistent.
It verifies that JSON examples are proper JSON, that API methods that accept or
return a specific resource type have valid request/response examples, and that
the metadata in the documentation is formatted properly.

Option | Description
---|---
--method <method_name> | Optional. Specify the name of a request method to evaluate. If missing, all methods are evaluated.

Example: `apidocs.exe check-docs --path ~/github/onedrive-api-docs --method search`

### Check-service Command
Check the documented requests and responses against an actual REST service. Requires
the URL for the service and an OAuth access token to call the service.

Option | Description
---|---
`--access-token "token"` | OAuth access token to use when calling the service. You can use set to provide a default value. You may need to escape the token value by enclosing it in double quotes.
`--url <url>` | Set the base URL for the service calls.
`--parameter-file <relative_path>` | Relative path to a JSON file describing the parameter replacements to make in the request methods.
`--pause` | Pause for a key press between API calls to the service to enable reading the responses.
--method <method_name> | Check a single request/response method instead of everything in the documentation.

Example:
```
apidocs set --access-token "asdkljasdkj..." -url https://api.onedrive.com/v1.0
apidocs check-service --parameter-file requests.json --method search
```

### Publish Command
The publish command creates a "clean" copy of the documentation set by running
it through a simple set of processing rules. The output can either be the original
format or you can convert the output to another supported format.

Option | Description
---|---
`--output <path>` | Required. Output directory for sanitized documentation.
`--extensions <value>` | Specify a common separated list of file extensions that are considered "documentation files" and should be processed. Default is `.md,.mdown`.
`--format [markdown,html]` | Specify the format for the output documentation. Either `markdown` (default) or `html`.
`--ignore-path <value>` | Specify a semicolon separated list of paths in the documentation that should never be copied to output. Default is `\internal;\.git;\.gitignore;\generate_html_output`.
`--include-all` | Default: true. Specify that all files, even those which are not in the extension list, are copied to the output location. This allows graphics and other non-text files to be copied.

Example: `apidocs --path ~/github/onedrive-api-docs --output ~/documents/onedrive-docs`

### Set Command
The set command lets you preset values for some parameters so they don't need to
be specified on each command line. These values are stored in the app.config
file next to the application executable.

Example: `apidocs set --path ~/github/onedrive-api-docs --url https://api.onedrive.com/v1.0`

Option | Description
---|---
`--path <path>` | Set the path to the documentation folder.
`--access-token "token"` | Set the access token used for calling the service.
`--url <url>` | Set the base URL for API calls.
`--reset` | Erase any stored values.
`--print` | Print any currently stored values.

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
  "name": "scenario",
  "method": "get-drive",
  "enabled": true,
  "values": [
    {
      "placeholder": "drive-id",
      "location": "url",
      "value": "F04AA961744A"
    }
  ],
  "values-from-request": {
    "request": "GET /drive",
    "values": [
      {
        "placeholder": drive-id",
        "location": "url"
        "path": "$.values[0].id"
      }
    ]
  }
}
```

Property | Type | Description
---|---|---
`name` | string | The name of the scenario described.
`method` | string | The name of the method this scenario uses. Either defined in the documentation or a substitute name is auto-generated.
`enabled` | bool | Enable or disable the scenario.
`values` | Array | Array of static placeholder values.
`values[#].placeholder` | string | The name of the placeholder. In `url` locations, the placeholder name is enclosed in braces `{drive-id}`.
`values[#].location` | string | One of the following values: `url`, `json`, or `body`. For `json` the placeholder value is used as a jsonPath query to where in a request object the value should be inserted. For `body` the value of the placeholder is substituted for the body of the request.
`values[#].value` | string | The value to use for the placeholder.
`values-from-request` | object | Provides an HTTP request that will be used to populate the values of the placeholders.
`values-from-request.request` | string | An HTTP request that is issued to the service. The request is parsed the same way that request methods are parsed. The service URL will be added to the root of the requested URL.
`values-from-request.values` | Array | An array of placeholders.
`values-from-request.values[#].placeholder` | string | Same as `values[#].placeholder`
`values-from-request.values[#].location` | string | Same as `values[#].location`
`values-from-request.values[#].path` | string | A jsonPath query expression for the value to use in the placeholder.

## Open Source
The API Documentation Test Tool uses the following open source components:

* [MarkdownDeep](https://github.com/toptensoftware/MarkdownDeep) - Markdown for C# parser. Apache 2.0 license, Copyright (C) 2010-2011 Topten Software.
* [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) - Json parser for .NET apps. MIT license, Copyright (c) 2007 James Newton-King
* [CommandLineParser](https://commandline.codeplex.com/) - Command line parser library. MIT license, Copyright (c) 2005 - 2012 Giacomo Stelluti Scala.
