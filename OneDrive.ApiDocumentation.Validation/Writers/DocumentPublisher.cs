namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.ComponentModel;
    using MarkdownDeep;
    using System.Linq;

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
        /// Indicates if non-text files are also published to the output directory.
        /// SkipPaths are still ignored regardless of this setting.
        /// </summary>
        public bool PublishAllFiles { get; set; }

        /// <summary>
        /// Output log
        /// </summary>
        public BindingList<ValidationError> Messages { get; private set; }

        /// <summary>
        /// Include verbose log messages in the output log
        /// </summary>
        public bool VerboseLogging { get; set; }

        #endregion

        protected List<string> scannableExtensions;
        protected List<string> ignoredPaths;
        protected string _outputFolder;

        #region Constructors

        public DocumentPublisher(DocSet docset)
		{
            Documents = docset;
            RootPath = new DirectoryInfo(docset.SourceFolderPath).FullName;
            if (!RootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                RootPath = string.Concat(RootPath, Path.DirectorySeparatorChar);

            SourceFileExtensions = ".md,.mdown";
            SkipPaths = "\\internal;\\.git;\\legacy;\\generate_html_docs;\\.gitignore;\\.gitattributes";
            Messages = new BindingList<ValidationError>();
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
            var eventHandler = NewMessage;
            if (null != eventHandler)
            {
                eventHandler(this, new ValidationMessageEventArgs(message));
            }
            Messages.Add(message);
        }
        #endregion

        /// <summary>
        /// Main entry point - publish the documentation set to the output folder.
        /// </summary>
        /// <param name="outputFolder"></param>
        /// <returns></returns>
		public virtual async Task PublishToFolderAsync(string outputFolder)
		{
            Messages.Clear();

            _outputFolder = outputFolder;

            DirectoryInfo destination = new DirectoryInfo(outputFolder);
            SnapVariables();

			await PublishFromDirectory(new DirectoryInfo(RootPath), destination);
		}

        protected void SnapVariables()
        {
            scannableExtensions = new List<string>(SourceFileExtensions.Split(','));
            ignoredPaths = new List<string>(SkipPaths.Split(';'));
        }

		/// <summary>
		/// Returns the relative directory for the passed directory based on the
		/// RootPath property.
		/// </summary>
		/// <returns>The directory path.</returns>
		/// <param name="dir">Dir.</param>
		protected string RelativeDirectoryPath(DirectoryInfo dir, bool includeRootSpecifier)
		{
			var fullPath = dir.FullName;
			if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				fullPath = string.Concat(fullPath, Path.DirectorySeparatorChar);
			}

			if (fullPath.Equals(RootPath))
			{
				return includeRootSpecifier ? Path.DirectorySeparatorChar.ToString() : string.Empty;
			}
			else if (fullPath.StartsWith(RootPath))
			{
				if (includeRootSpecifier)
					return Path.DirectorySeparatorChar + fullPath.Substring(RootPath.Length);
				else
					return fullPath.Substring(RootPath.Length);
			}

			Debug.Assert(false, "Failed to find a relative path for {0} from {1}", dir.FullName, RootPath);
			return null;
		}

		/// <summary>
        /// PublishFromDirectory iterates over every file in the documentation set root folder and below
        /// and calls IsInternalPath, IsScannableFile, CopyToOutput, or PublishFileToDetination.
		/// </summary>
		/// <param name="directory">Directory.</param>
		protected virtual async Task PublishFromDirectory(DirectoryInfo directory, DirectoryInfo destinationRoot)
		{
			var pathDisplayName = RelativeDirectoryPath(directory, true);

			var filesInDirectory = directory.GetFiles();
			if (filesInDirectory.Length > 0)
			{
				// Create folder in the destination

				var relativePath = RelativeDirectoryPath(directory, false);
                var newDirectoryPath = Path.Combine(destinationRoot.FullName, relativePath);
				var newDirectory = new DirectoryInfo(newDirectoryPath);
				if (!newDirectory.Exists)
				{
                    LogMessage(new ValidationMessage(pathDisplayName, "Creating new directory in output folder."));
					newDirectory.Create();
				}
			}

            ConfigureOutputDirectory(destinationRoot);

			foreach (var file in filesInDirectory)
			{
				if (IsInternalPath(file))
				{
                    LogMessage(new ValidationMessage(file.Name, "Source file was on the internal path list, skipped."));
				}
				else if (IsScannableFile(file))
				{
                    var docFile = Documents.Files.Where(x => x.FullPath.Equals(file.FullName)).FirstOrDefault();
                    PageAnnotation annotation = null;
                    if (null != docFile)
                    {
                        annotation = docFile.Annotation;
                    }

					await PublishFileToDestination(file, destinationRoot, annotation);
				}
				else if (CopyToOutput(file))
				{
					CopyFileToDestination(file, destinationRoot);
				}
				else
				{
                    LogMessage(new ValidationWarning(ValidationErrorCode.ExtraFileDetected, file.Name, "Source file was not in the scan or copy list, skipped."));
				}
			}

			var subfolders = directory.GetDirectories();
			foreach (var folder in subfolders)
			{
				if (IsInternalPath(folder))
				{
                    LogMessage(new ValidationMessage(folder.Name, "Source file was on the internal path list, skipped."));
					// Skip output for that directory
					continue;
				}
				else
				{
					var displayName = RelativeDirectoryPath(folder, true);
                    LogMessage(new ValidationMessage(displayName, "Scanning directory."));
					await PublishFromDirectory(folder, destinationRoot);
				}
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
        /// <param name="file">File.</param>
        /// <param name="destinationRoot">Destination root.</param>
        /// <param name="newExtension">New extension.</param>
        protected virtual string GetPublishedFilePath(FileInfo sourceFile, DirectoryInfo destinationRoot, string changeFileExtension = null)
		{
			var relativePath = RelativeDirectoryPath(sourceFile.Directory, false);

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
				var outPath = GetPublishedFilePath(file, destinationRoot);
                LogMessage(new ValidationMessage(file.Name, "Copying to output directory without scanning."));
                file.CopyTo(outPath, true);
			}
			catch (Exception ex)
			{
                LogMessage(new ValidationError(ValidationErrorCode.ErrorCopyingFile, file.Name, "Cannot copy file to output directory: {0}", ex.Message));
			}
		}

		/// <summary>
		/// Scans the text content of a file and removes any "internal" comments/references
		/// </summary>
		/// <param name="file">File.</param>
		protected virtual async Task PublishFileToDestination(FileInfo sourceFile, DirectoryInfo destinationRoot, PageAnnotation pageData)
		{
            throw new NotImplementedException("This method is not implemented in the abstract class.");
		}

		#region Scanning Rules

		[ScanRuleAttribute(ScanRuleTarget.LineOfText)]
		protected bool IsDoubleBlockQuote(string text)
		{
			return text.StartsWith(">>") || text.StartsWith(" >>");
		}

		[ScanRuleAttribute(ScanRuleTarget.FileInfo)]
		public bool IsScannableFile(FileInfo file)
		{
			return scannableExtensions.Contains(file.Extension);
		}

		public bool CopyToOutput(FileInfo file)
		{
			return PublishAllFiles;
		}

		[ScanRuleAttribute(ScanRuleTarget.FileInfo)]
		public bool IsInternalPath(DirectoryInfo folder)
		{
			var relativePath = RelativeDirectoryPath(folder, true);
			return IsRelativePathInternal(relativePath);
		}

		[ScanRuleAttribute(ScanRuleTarget.FileInfo)]
		public bool IsInternalPath(FileInfo file)
		{
			var relativePath = Path.Combine(RelativeDirectoryPath(file.Directory, true), file.Name);
			return IsRelativePathInternal(relativePath);
		}

		protected bool IsRelativePathInternal(string relativePath)
		{
			var pathComponents = relativePath.Split(new char[] {Path.DirectorySeparatorChar},
				StringSplitOptions.RemoveEmptyEntries);
			var pathSyntax = "\\" + pathComponents.ComponentsJoinedByString("\\");

            var query = from p in ignoredPaths
                        where pathSyntax.StartsWith(p)
                        select p;

            return query.FirstOrDefault() != null;
		}

        protected bool IsDocFileInternal(DocFile file)
        {
            return IsRelativePathInternal(file.DisplayName);
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
            Message = message;
        }
    }

	public enum ScanRuleTarget
	{
		DirectoryInfo,
		FileInfo,
		LineOfText
	}
}
