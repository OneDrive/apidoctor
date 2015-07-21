namespace ApiDocs.Validation.Writers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using ApiDocs.Validation.Error;

    public abstract class DocumentPublisher
	{
        #region Properties
        /// <summary>
        /// The document set that is the source for the publisher
        /// </summary>
        public DocSet Documents { get; private set; }

        /// <summary>
        /// Comma separated list of file extensions that should be scanned for internal content
        /// </summary>
        public string SourceFileExtensions { get; set; }

        /// <summary>
        /// Full path to the source folder for documentation
        /// </summary>
        public string RootPath { get; private set; }

        /// <summary>
        /// Semicolon separated values of pathes that should be excluded from publishing.
        /// </summary>
        public string SkipPaths { get; set; }

        /// <summary>
        /// Allows converting the line endings for markdown documents to another format.
        /// </summary>
        public LineEndings OutputLineEndings { get; set; }

        /// <summary>
        /// Output log
        /// </summary>
        public BindingList<ValidationError> Messages { get; private set; }

        /// <summary>
        /// Include verbose log messages in the output log
        /// </summary>
        public bool VerboseLogging { get; set; }

        #endregion

        protected List<string> ScannableExtensions;
        protected List<string> IgnoredPaths;
        protected string OutputFolder;

        #region Constructors

	    protected DocumentPublisher(DocSet docset)
		{
	        this.Documents = docset;
	        this.RootPath = new DirectoryInfo(docset.SourceFolderPath).FullName;
            if (!this.RootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                this.RootPath = string.Concat(this.RootPath, Path.DirectorySeparatorChar);

	        this.SourceFileExtensions = ".md,.mdown";
	        this.SkipPaths = "\\.git;\\.gitignore;\\.gitattributes";
	        this.OutputLineEndings = LineEndings.Default;
	        this.Messages = new BindingList<ValidationError>();
		}
        #endregion

        #region Logging
        
        public event EventHandler<ValidationMessageEventArgs> NewMessage;

        /// <summary>
        /// Logs out a validation message.
        /// </summary>
        /// <param name="message">Message.</param>
        protected void LogMessage(ValidationError message)
        {
            var eventHandler = this.NewMessage;
            if (null != eventHandler)
            {
                eventHandler(this, new ValidationMessageEventArgs(message));
            }
            this.Messages.Add(message);
        }
        #endregion

        /// <summary>
        /// Main entry point - publish the documentation set to the output folder.
        /// </summary>
        /// <param name="outputFolder"></param>
        /// <returns></returns>
		public virtual async Task PublishToFolderAsync(string outputFolder)
		{
            this.Messages.Clear();

            this.OutputFolder = outputFolder;

            DirectoryInfo destination = new DirectoryInfo(outputFolder);
            this.SnapVariables();

			await this.PublishFromDirectoryAsync(new DirectoryInfo(this.RootPath), destination);
		}

        protected void SnapVariables()
        {
            this.ScannableExtensions = new List<string>(this.SourceFileExtensions.Split(','));
            this.IgnoredPaths = new List<string>(this.SkipPaths.Split(';'));
        }

	    /// <summary>
	    /// Returns the relative directory for the passed directory based on the
	    /// RootPath property.
	    /// </summary>
	    /// <returns>The directory path.</returns>
	    /// <param name="dir">Dir.</param>
	    /// <param name="includeRootSpecifier"></param>
	    protected string RelativeDirectoryPath(DirectoryInfo dir, bool includeRootSpecifier)
		{
			var fullPath = dir.FullName;
			if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				fullPath = string.Concat(fullPath, Path.DirectorySeparatorChar);
			}

			if (fullPath.Equals(this.RootPath))
			{
				return includeRootSpecifier ? Path.DirectorySeparatorChar.ToString() : string.Empty;
			}
			else if (fullPath.StartsWith(this.RootPath))
			{
				if (includeRootSpecifier)
					return Path.DirectorySeparatorChar + fullPath.Substring(this.RootPath.Length);
				else
					return fullPath.Substring(this.RootPath.Length);
			}

			Debug.Assert(false, "Failed to find a relative path for {0} from {1}", dir.FullName, this.RootPath);
			return null;
		}

	    /// <summary>
	    /// PublishFromDirectory iterates over every file in the documentation set root folder and below
	    /// and calls IsInternalPath, IsScannableFile, CopyToOutput, or PublishFileToDetination.
	    /// </summary>
	    /// <param name="directory">Directory.</param>
	    /// <param name="destinationRoot"></param>
	    protected virtual async Task PublishFromDirectoryAsync(DirectoryInfo directory, DirectoryInfo destinationRoot)
		{
			var pathDisplayName = this.RelativeDirectoryPath(directory, true);

			var filesInDirectory = directory.GetFiles();
			if (filesInDirectory.Length > 0)
			{
				// Create folder in the destination

				var relativePath = this.RelativeDirectoryPath(directory, false);
                var newDirectoryPath = Path.Combine(destinationRoot.FullName, relativePath);
				var newDirectory = new DirectoryInfo(newDirectoryPath);
				if (!newDirectory.Exists)
				{
				    this.LogMessage(new ValidationMessage(pathDisplayName, "Creating new directory in output folder."));
					newDirectory.Create();
				}
			}

		    this.ConfigureOutputDirectory(destinationRoot);

			foreach (var file in filesInDirectory)
			{
				if (this.IsInternalPath(file))
				{
				    this.LogMessage(new ValidationMessage(file.Name, "Source file was on the internal path list, skipped."));
				}
				else if (this.IsMarkdownFile(file))
				{
                    var docFile = this.Documents.Files.FirstOrDefault(x => x.FullPath.Equals(file.FullName));
					await this.PublishFileToDestinationAsync(file, destinationRoot, docFile);
				}
				else
				{
				    this.CopyFileToDestination(file, destinationRoot);
				}
			}

			var subfolders = directory.GetDirectories();
			foreach (var folder in subfolders)
			{
				if (this.IsInternalPath(folder))
				{
				    this.LogMessage(new ValidationMessage(folder.Name, "Source file was on the internal path list, skipped."));
					// Skip output for that directory
					continue;
				}
			    
                var displayName = this.RelativeDirectoryPath(folder, true);
			    this.LogMessage(new ValidationMessage(displayName, "Scanning directory."));
			    await this.PublishFromDirectoryAsync(folder, destinationRoot);
			}
		}

        /// <summary>
        /// Called before anything happens to configure the root destination folder
        /// </summary>
        /// <param name="destinationRoot"></param>
        protected virtual void ConfigureOutputDirectory(DirectoryInfo destinationRoot)
        {
            // Nothing to do
        }

        /// <summary>
        /// Convert an input file into a path in the destinationRoot. Optionally
        /// giving the file a new file extension.
        /// </summary>
        /// <returns>The file path.</returns>
        /// <param name="sourceFile">File.</param>
        /// <param name="destinationRoot">Destination root.</param>
        /// <param name="changeFileExtension">New extension.</param>
        protected virtual string GetPublishedFilePath(FileInfo sourceFile, DirectoryInfo destinationRoot, string changeFileExtension = null)
		{
			var relativePath = this.RelativeDirectoryPath(sourceFile.Directory, false);

            string destinationFileName = sourceFile.Name;
            if (!string.IsNullOrEmpty(changeFileExtension))
            {
                var fileNameNoExtension = Path.GetFileNameWithoutExtension(sourceFile.Name);
                destinationFileName = string.Concat(fileNameNoExtension, changeFileExtension);
            }

            var outputPath = Path.Combine(destinationRoot.FullName, relativePath, destinationFileName);
			return outputPath;
		}

        /// <summary>
        /// Copies a file contained in the documentation set to the destination root at the same level of hierarchy.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="destinationRoot"></param>
		protected virtual void CopyFileToDestination(FileInfo file, DirectoryInfo destinationRoot)
		{
			try
			{
				var outPath = this.GetPublishedFilePath(file, destinationRoot);
			    this.LogMessage(new ValidationMessage(file.Name, "Copying to output directory without scanning."));
                file.CopyTo(outPath, true);
			}
			catch (Exception ex)
			{
			    this.LogMessage(new ValidationError(ValidationErrorCode.ErrorCopyingFile, file.Name, "Cannot copy file to output directory: {0}", ex.Message));
			}
		}

	    /// <summary>
	    /// Scans the text content of a file and removes any "internal" comments/references
	    /// </summary>
	    /// <param name="sourceFile">File.</param>
	    /// <param name="destinationRoot"></param>
	    /// <param name="page"></param>
#pragma warning disable 1998
	    protected virtual async Task PublishFileToDestinationAsync(FileInfo sourceFile, DirectoryInfo destinationRoot, DocFile page)
#pragma warning restore 1998
		{
            throw new NotImplementedException("This method is not implemented in the abstract class.");
		}

        /// <summary>
        /// Convert the line endings of a string to another format.
        /// </summary>
        /// <param name="inputText"></param>
        /// <param name="endings"></param>
        /// <returns></returns>
        protected virtual async Task<string> ConvertLineEndingsAsync(string inputText, LineEndings endings)
        {
            const char carriageReturnChar = (char)13;
            const char lineFeedChar = (char)10;
            StringWriter writer = new StringWriter();
            switch(endings)
            {
                case LineEndings.Macintosh:
                    writer.NewLine = carriageReturnChar.ToString();
                    break;
                case LineEndings.Unix:
                    writer.NewLine =  lineFeedChar.ToString();
                    break;
                case LineEndings.Windows:
                    writer.NewLine = new string(new char[] { carriageReturnChar, lineFeedChar});
                    break;
            }

            if (this.OutputLineEndings != LineEndings.Default)
            {
                await Task.Run(() =>
                {
                    StringReader reader = new StringReader(inputText);
                    string currentLine;
                    while ((currentLine = reader.ReadLine()) != null)
                    {
                        writer.WriteLine(currentLine);
                    }
                });
                return writer.ToString();
            }
            else
            {
                return inputText;
            }
        }

		#region Scanning Rules

		[ScanRule(ScanRuleTarget.LineOfText)]
		protected bool IsDoubleBlockQuote(string text)
		{
			return text.StartsWith(">>") || text.StartsWith(" >>");
		}

		[ScanRule(ScanRuleTarget.FileInfo)]
		public bool IsMarkdownFile(FileInfo file)
		{
			return this.ScannableExtensions.Contains(file.Extension);
		}

		[ScanRule(ScanRuleTarget.FileInfo)]
		public bool IsInternalPath(DirectoryInfo folder)
		{
			var relativePath = this.RelativeDirectoryPath(folder, true);
			return this.IsRelativePathInternal(relativePath);
		}

		[ScanRule(ScanRuleTarget.FileInfo)]
		public bool IsInternalPath(FileInfo file)
		{
			var relativePath = Path.Combine(this.RelativeDirectoryPath(file.Directory, true), file.Name);
			return this.IsRelativePathInternal(relativePath);
		}

		protected bool IsRelativePathInternal(string relativePath)
		{
			var pathComponents = relativePath.Split(new char[] {Path.DirectorySeparatorChar},
				StringSplitOptions.RemoveEmptyEntries);
			var pathSyntax = "\\" + pathComponents.ComponentsJoinedByString("\\");

            var query = from p in this.IgnoredPaths
                        where pathSyntax.StartsWith(p)
                        select p;

            return query.FirstOrDefault() != null;
		}

        protected bool IsDocFileInternal(DocFile file)
        {
            return this.IsRelativePathInternal(file.DisplayName);
        }

		#endregion

	}
        
	public class ScanRuleAttribute : Attribute
	{
		public ScanRuleTarget Target {get; set;}

		public ScanRuleAttribute(ScanRuleTarget target)
		{
			this.Target = target;
		}
	}

    public class ValidationMessageEventArgs : EventArgs
    {
        public ValidationError Message { get; private set; }

        public ValidationMessageEventArgs(ValidationError message)
        {
            this.Message = message;
        }
    }

	public enum ScanRuleTarget
	{
		DirectoryInfo,
		FileInfo,
		LineOfText
	}

    public enum LineEndings
    {
        Default,
        Windows,
        Unix,
        Macintosh
    }
}
