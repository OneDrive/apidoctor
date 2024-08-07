# Kibali Permissions Tooling

This repository contains a library and a commandline tool for managing AuthZ permissions.  This was built to 
help manage permissions for Microsoft Graph.


From the repo root you can build the kibali tool using the following command:

```shell
dotnet build
```

You can create the Kibali permissions file from the Graph Explorer permissions metadata data using the following command:

```shell
.\kibaliTool\bin\Debug\net8.0\KibaliTool.exe import
```

This command will output a file called GraphPermissions.json in the `.\output` folder. Once you have this file you
can query the file for permissions using the following command:

```shell
.\kibaliTool\bin\Debug\net8.0\KibaliTool.exe query --pf .\output\GraphPermissions.json --url "/me/messages"
```
