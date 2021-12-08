---
title: "example resource"
description: "Represents an example resource"
author: "apidoctor"
ms.localizationpriority: high
ms.prod: "users"
doc_type: resourcePageType
---

# example resource type

Namespace: microsoft.graph

Represents a Resource

## Resource with ISO 8601 timestamp, URL, and enumerated values.

<!-- { "blockType": "resource", "@odata.type": "example.resource" } -->
```json
{
	"year": 1234,
	"downloadUrl": "url",
	"createdDateTime": "timestamp",
	"season": "summer | fall | winter | spring",
	"ownerName": "rgregg",
	"contentType": "string"
}
```

### Properties

| Name            | Type      | Description
|:----------------|:----------|:--------------
| year            | int       | the year
| downloadUrl     | string    | download url
| createdDateTime | timestamp | created date time
| season          | season    | season enum
| ownerName       | string    | name of the owner
| contentType     | string    | mimetype

#### season values

| Value
|:---------
| summer
| fall
| winter
| spring

## Example request/response that's completely valid

<!-- { "blockType": "request", "name": "valid-response" } -->
```http
GET /timestamp
```

<!-- { "blockType": "response", "name": "valid-response", "@odata.type": "example.resource", "truncated": true } -->
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "year": 2015,
  "downloadUrl": "https://foobar.com/something/another",
  "createdDateTime": "2015-07-08T15:46:00Z",
  "season": "summer",
  "ownerName": "Ryan Gregg",
  "contentType": "text/plain"
}
```

## Example request/response that has invalid date format

<!-- { "blockType": "request", "name": "bad-timestamp" } -->
```http
GET /timestamp
```

<!-- { "blockType": "response", "name": "bad-timestamp", "@odata.type": "example.resource", "truncated": true } -->
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "createdDateTime": "July 8, 2015 3:48 PM"
}
```

## Example request/response that has invalid URL format

<!-- { "blockType": "request", "name": "bad-url" } -->
```http
GET /timestamp
```

<!-- { "blockType": "response", "name": "bad-url", "@odata.type": "example.resource", "truncated": true } -->
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "downloadUrl": "../something/another"
}
```


## Example request/response that has invalid enumerated value

<!-- { "blockType": "request", "name": "bad-enum-value" } -->
```http
GET /timestamp
```

<!-- { "blockType": "response", "name": "bad-enum-value", "@odata.type": "example.resource", "truncated": true } -->
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "season": "jupiter"
}
```