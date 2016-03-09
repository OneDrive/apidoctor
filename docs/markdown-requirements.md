# Markdown requirements

Markdown-scanner tries to be a very flexible tool to ensure that the format
of the documentation does not have strict requirements. This allows the tool
to be used with the widest range of documentation sources.

However, some features do require specific attributes / concepts to exist in
the markdown content.

To work with this tool, the source documentation has a few basic requirements:

* Documentation must be in markdown format.
* Requests and responses in the documentation are full HTTP style examples.
* Requests, responses, and resources are enclosed in fenced code blocks (three backticks ` ``` `).
* Requests, responses, and resources have simple metadata enclosed in an HTML comment immediately before the codeblock.

## Formatting

Markdown supports the GitHub flavored markdown format. This is less useful for
the automated test scenarios, but is required for HTML publishing.

## Code blocks

Markdown scanner recognizes fenced code blocks with three back-tick characters.
These fenced delimiters must be on a line by themselves, otherwise the tool will
not properly recognize the code block.

Code blocks can include a language specifier after the first fenced delimiter.
For example:

```json
{
  "property": "value"
}
```

These code block language attributes are used to auto-detect how to interpret
a code block and published into the HTML to enable highlight.js or similar
systems to provide code syntax highlighting in published documentation.

## Code block attributes

Each code block that represents a meaningful concept needs to include an annotation
as an HTML comment block immediately preceding the code block. This comment should
include a JSON object that defines the parameters for the following code block.

The following properties are defined for code block annotations:

| Name                   | Value            | Description                                                                                                                                                                  |
|:-----------------------|:-----------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **blockType**          | String           | Define the type of code block. Possible values are: `request`, `response`, `resource`, `example`, and `ignored`                                                              |
| **name**               | String           | The name of a request or response. This is used to pair scenarios to request/response methods.                                                                               |
| **@odata.type**        | Resource name    | Specify the resource type for a resource block, or the resource type for the body of a request, response, or example.                                                        |
| **optionalProperties** | Array of strings | For a resource, define which properties are optional. Optional properties are not required in the usage of the resource unless they are also shown in the expected response. |
| **isCollection**       | Boolean          | If true, indicates that the request or response body is a collection of the resource type specified.                                                                         |
| **collectionProperty** | String           | The name of the property that holds the collection, if isCollection is true. This defaults to `value`.                                                                       |
| **isEmpty**            | Boolean          | If true, indicates that the response block should not contain a body. This is useful for API calls that expect to return a 204 No Content.                                   |
| **truncated**          | Boolean          | If true, indicates that the example of the resource provided does not include all required fields, and that state shouldn't generate errors or warnings.                     |
| **expectError**        | Boolean          | If true, indicates that the response should be an error response and not the standard resource response expected.                                                            |
| **nullableProperties** | Array of strings | For a resource, define which properties can return null values. By default Markdown-scanner expects no null properties to be returned.                                       |
| **scopes**             | String           | A space-seperated value of scopes which are required for this method to be useful. All scopes listed are required to be provided by the account for this method to be run. A warning is generated if the account doesn't have the required scopes.  |

## Pairing requests and responses

Markdown-scanner assumes that request and response blocks always come in pairs
and that the first response block encountered after a request block should be paired
with that request.

This behavior can be overridden by specifying the **name** property on both the
request and response block with the same value, so they are paired together.

## Examples
An example usage would look like this in the markdown:

```
### Resource Definition

<!-- {"blockType": "resource", "@odata.type": "example_item"} -->
\```
{
  "name": "string",
  "count": 123,
  "season": "summer | fall | winter | spring",
  "webUrl": "url",
  "createdDateTime": "datetime"
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
  "name": "root_folder",
  "count": 4,
  "height": "6.12",
  "season": "summer",
  "webUrl": "http://example.org",
  "createdDateTime": "2015-07-15T14:44:00Z"
}
\```
```

This file, if included in the documentation, would be read as one resource,
`example_item` that has a JSON object schema with these properties:

| Property name     | Type        | Validation type                                                                        |
|:------------------|:------------|:---------------------------------------------------------------------------------------|
| `name`            | string      | Value is only validated to be a string type.                                           |
| `count`           | integer     | Value is only validated to be an integer type.                                         |
| `height`          | float       | Value is only validated to be a float type.                                            |
| `season`          | enum-string | Value is validated to be one of the enumerated values (separated by a pipe character). |
| `webUrl`          | url         | Value is validated to be an absolute URL.                                              |
| `createdDateTime` | datetime    | Value is validated to be a string value in the format of an ISO8601 date time stamp.   |

It then has one request method which calls
`GET <url_root>/drive/items/root` and returns an `example_item` resource.

Using `check-docs` would verify that the return example in the documentation
matches the proper schema. Using `check-service` would verify that the service
responses to the request following the schema.

Request/response pairs are identified by matching up the next response found
after the request codeblock. You can have other codeblocks between a request and
a response as long as they are missing the metadata or tagged as
`"blockType": "ignored"`. A given file can have as many resources and
request/response pairs as necessary.
