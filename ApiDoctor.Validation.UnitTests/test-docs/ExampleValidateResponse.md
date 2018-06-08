<!-- { "blockType": "resource", "@odata.type": "test.simple" } -->
```json
{
  "prop1": "testing",
  "prop2": "simple",
  "prop3": "waterbottle"
}
```

Here's an example where the server response should be verified against
properties in the sample, even though we're truncating the results:

<!-- { "blockType": "request", "name": "simple-truncation-test" } -->
```http
GET /test_resource
```

Here's the expected response, as written in the documentation. No validation errors
occur here because truncated: true.

<!-- { "blockType": "response", "@odata.type": "test.simple", "truncated": true } -->
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "prop1": "foobar",
  "prop2": "another something"
}
```

Here's the simulated server response. Even though truncated: true is set for the expected response
this should error because the set of properties in this example doesn't match the expected
response.

All properties in the expected response are always required.

<!-- { "blockType": "simulatedResponse" } -->
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "prop1": "foobar",
}
```