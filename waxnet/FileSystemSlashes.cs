using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet
{
	class FileSystemSlashes
	{
		private const string FILE_SYSTEM_SLASH = @"\";
		private const string WEB_SAFE_SLASH = "/";

		public static string EnsureFileSystemTrailingSlash(string input)
		{
			input = EnsureFilesystemSlashes(input);
			if (!input.EndsWith(FILE_SYSTEM_SLASH))
			{
				input = input + FILE_SYSTEM_SLASH;
			}
			return input;
		}

		public static string EnsureFilesystemSlashes(string input)
		{
			input = EnsureSlashes(input, FILE_SYSTEM_SLASH);
			return input;
		}

		public static string EnsureWebSafeSlashes(string input)
		{
			input = EnsureSlashes(input, WEB_SAFE_SLASH);
			return input;
		}

		public static string EnsureSlashes(string input, string slash)
		{
			input = input.Replace(FILE_SYSTEM_SLASH, slash);
			input = input.Replace(WEB_SAFE_SLASH, slash);
			return input;
		}
	}
}
