<!-- { "blockType": "resource", "@odata.type": "oneDrive.item" } -->
```json
{
	"id": "string",
	"lastModifiedDateTime": "datetime",
	"name": "string",
	"size": 218753122201,
	"webUrl": "url",
	"children": [
		{ "@odata.type": "oneDrive.item" }
	]
}
```

<!-- { "blockType": "request", "name": "drive-plus-children" } -->
```http
GET /drive/root?expand=children(select=id,name)
```

The request returns the collection items, with the children collection expanded.

<!-- { "blockType": "response", "@odata.type": "oneDrive.item", "truncated": true } -->
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "root",
  "lastModifiedDateTime": "2013-06-20T02:54:44.547Z",
  "name": "root",
  "size": 218753122201,
  "webUrl": "https://onedrive.live.com/?cid=0f040...",
  "children": [
    {
      "id": "F04AA961744A809!48443",
      "name": "Applications",
    },
    {
      "id": "F04AA961744A809!92647",
      "name": "Attachments",
    },
    {
      "id": "F04AA961744A809!93269",
      "name": "Balsmiq Sketches",
    },
    {
      "id": "F04AA961744A809!65191",
      "name": "Camera imports",
    }
  ]
}
```


<!-- { "blockType": "simulatedResponse", "@odata.type": "oneDrive.item", "truncated": true } -->
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "root",
  "lastModifiedDateTime": "2013-06-20T02:54:44.547Z",
  "name": "root",
  "size": 218753122201,
  "webUrl": "https://onedrive.live.com/?cid=0f040...",
  "children": [
    {
      "id": "F04AA961744A809!48443"
    },
    {
      "id": "F04AA961744A809!92647"
    },
    {
      "id": "F04AA961744A809!93269"
    },
    {
      "id": "F04AA961744A809!65191"
    }
  ]
}
```
