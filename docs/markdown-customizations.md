# Markdown customizations

Markdown-scanner implements the following additional features on top of the Markdown language.

- [Tagging of content](#tagging-of-content)
- [Including other Markdown files](#including-other-markdown-files)

## Tagging of content

Tagging of content allows the author to include sections that are conditionally included or omitted based on the parameters passed at build time. This allows one set of source files to generate content for multiple targets.

### Syntax

To tag content, wrap the Markdown to tag with the following tags:

```Markdown
[TAGS=<Comma-delimited list of tags>]

Some Markdown content here

[END]
```

This will cause everything between the start `[TAGS]` line and the ending `[END]` line to only be included in the final output if one or more of the tags in the comma-delimited list are specified at build time.

Included content will be wrapped with `<div>` tags with a `class` attribute set based on the tags. The class value takes the form `content-<TAG VALUE>`. So a Markdown marker of `[TAGS=FOO]` would result in `<div class="content-foo">`.

#### Limitations

There are some limits to what you can do with this.

- Both the `[TAGS]` marker and the `[END]` marker must be on their own line. You cannot insert them in the middle of a paragraph.

### Specifying tags at build time

To specify tags at build time, the `--parameters` parameter must include a `TAGS` key, with the value set to a comma-delimited list of tags to include. For example:

```Shell
apidocs.exe publish --format html --path .\src --output .\out --parameters "TAGS=OUTLOOK,v2"
```

### Example

Let's take a look at a simple example. Suppose you have the following Markdown source file:

```Markdown
# Tagging Demo

This content should always appear in the output because it is not tagged.

[TAGS=V1]
This is v1 content, and only appears if the V1 tag is specified at build.
[END]

[TAGS=V2]
This is v2 content, and only appears if the V2 tag is specified at build.
[END]

[TAGS=V1,V2]
This is v1 and v2 content, and only appears if either the V1 or V2 tags are specified at build. Also appears if both tags are specified.
[END]
```

If the `--parameters` parameter is omitted, or does not contain a `TAGS` key, or the `TAGS` key does not contain a `V1` or `V2` value, the following is the result:

```html
<h1 id="tagging-demo">Tagging Demo</h1>
<p>This content should always appear in the output because it is not tagged.</p>
```

If the `--parameters` parameter is set to `TAGS=V1`, the following is the result:

```html
<h1 id="tagging-demo">Tagging Demo</h1>
<p>This content should always appear in the output because it is not tagged.</p>
<div class="content-v1">
<p>This is v1 content, and only appears if the V1 tag is specified at build.</p>
</div>
<div class="content-v1 content-v2">
<p>This is v1 and v2 content, and only appears if either the V1 or V2 tags are specified at build. Also appears if both tags are specified.</p>
</div>
```

## Including other Markdown files

Use the following syntax to include another Markdown file.

```Markdown
[INCLUDE [<name of file>](path to file)]
```

For example:

```Markdown
[INCLUDE [included-file.md](includes/included-file.md)]
```

### Paths to include files

You can construct paths to include files as either relative to the root of the documentation set, or relative to the current file.

#### Relative to root

Paths that start with a `/` are considered relative to the root of the documentation set.

```Markdown
[INCLUDE [included-file.md](/includes/included-file.md)]
```

#### Relative to the current file

Paths that start with one or more `.`, or alpha-numeric characters, are considered relative to the current file.

```Markdown
[INCLUDE [included-file.md](./includes/included-file.md)]
[INCLUDE [included-file.md](includes/included-file.md)]
```