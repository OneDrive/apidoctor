using System;
using System.Drawing;

namespace Kibali;

public class PermissionsError
{
    public string Message { get; set; }

    public string Path { get; set; }

    public PermissionsErrorCode ErrorCode { get; set; }

    public override bool Equals(object obj)
    {
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            PermissionsError error = (PermissionsError)obj;
            return (Message == error.Message) && (Path == error.Path) && (ErrorCode == error.ErrorCode);
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Message, Path, ErrorCode);
    }

    public override string ToString()
    {
        return $"Kibali.PermissionsError.{this.ErrorCode};Message={this.Message};Path={this.Path};";
    }
}
