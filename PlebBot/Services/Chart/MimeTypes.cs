using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlebBot.Services.Chart
{
    /// <summary>
    /// Helper class to identify file type by the file header, not file extension.
    /// </summary>
    public static class MimeTypes
    {
        // all the file types to be put into one list
        public static List<FileType> types;

        static MimeTypes()
        {
            types = new List<FileType>
            {
                JPEG,
                PNG,
                GIF,
                BMP,
                ICO,
            };
        }

        #region Constants

        // file headers are taken from here:
        //http://www.garykessler.net/library/file_sigs.html
        //mime types are taken from here:
        //http://www.webmaster-toolkit.com/mime-types.shtml

        // graphics
        #region Graphics jpeg, png, gif, bmp, ico

        public readonly static FileType JPEG = new FileType(new byte?[] { 0xFF, 0xD8, 0xFF }, "jpg", "image/jpeg");
        public readonly static FileType PNG = new FileType(new byte?[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "png", "image/png");
        public readonly static FileType GIF = new FileType(new byte?[] { 0x47, 0x49, 0x46, 0x38, null, 0x61 }, "gif", "image/gif");
        public readonly static FileType BMP = new FileType(new byte?[] { 66, 77 }, "bmp", "image/gif");
        public readonly static FileType ICO = new FileType(new byte?[] { 0, 0, 1, 0 }, "ico", "image/x-icon");

        #endregion

        // number of bytes we read from a file
        public const int MaxHeaderSize = 560;  // some file formats have headers offset to 512 bytes
       
        #endregion

        #region Main Methods

        public static void SaveToXmlFile(string path)
        {
            using (FileStream file = File.OpenWrite(path))
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(types.GetType());
                serializer.Serialize(file, types);
            }
        }

        public static void LoadFromXmlFile(string path)
        {
            using (FileStream file = File.OpenRead(path))
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(types.GetType());
                List<FileType> tmpTypes = (List<FileType>)serializer.Deserialize(file);
                foreach (var type in tmpTypes)
                    types.Add(type);
            }
        }

        /// <summary>
        /// Read header of bytes and depending on the information in the header
        /// return object FileType.
        /// Return null in case when the file type is not identified. 
        /// Throws Application exception if the file can not be read or does not exist
        /// </summary>
        /// <remarks>
        /// A temp file is written to get a FileInfo from the given bytes.
        /// If this is not intended use 
        /// 
        ///     GetFileType(() => bytes); 
        ///     
        /// </remarks>
        /// <param name="file">The FileInfo object.</param>
        /// <returns>FileType or null not identified</returns>
        public static FileType GetFileType(this byte[] bytes)
        {
            return GetFileType(new MemoryStream(bytes));
        }

        /// <summary>
        /// Read header of a stream and depending on the information in the header
        /// return object FileType.
        /// Return null in case when the file type is not identified. 
        /// Throws Application exception if the file can not be read or does not exist
        /// </summary>
        /// <param name="file">The FileInfo object.</param>
        /// <returns>FileType or null not identified</returns>
        public static FileType GetFileType(this Stream stream)
        {
            FileType fileType = null;
            var fileName = Path.GetTempFileName();

            try
            {
                using (var fileStream = File.Create(fileName))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }
                fileType = GetFileType(new FileInfo(fileName));
            }
            finally
            {
                File.Delete(fileName);
            }
            return fileType;
        }

        /// <summary>
        /// Read header of a file and depending on the information in the header
        /// return object FileType.
        /// Return null in case when the file type is not identified. 
        /// Throws Application exception if the file can not be read or does not exist
        /// </summary>
        /// <param name="file">The FileInfo object.</param>
        /// <returns>FileType or null not identified</returns>
        public static FileType GetFileType(this FileInfo file)
        {
            return GetFileType(() => ReadFileHeader(file, MaxHeaderSize), file.FullName);
        }

        /// <summary>
        /// Read header of a file and depending on the information in the header
        /// return object FileType.
        /// Return null in case when the file type is not identified. 
        /// Throws Application exception if the file can not be read or does not exist
        /// </summary>
        /// <param name="fileHeaderReadFunc">A function which returns the bytes found</param>
        /// <param name="fileFullName">If given and file typ is a zip file, a check for docx and xlsx is done</param>
        /// <returns>FileType or null not identified</returns>
        public static FileType GetFileType(Func<byte[]> fileHeaderReadFunc, string fileFullName = "")
        {
            // if none of the types match, return null
            FileType fileType = null;

            // read first n-bytes from the file
            byte[] fileHeader = fileHeaderReadFunc();

            // checking if it's binary (not really exact, but should do the job)
            // shouldn't work with UTF-16 OR UTF-32 files
            // compare the file header to the stored file headers
            foreach (FileType type in types)
            {
                int matchingCount = GetFileMatchingCount(fileHeader, type);
                if (matchingCount == type.Header.Length)
                {
                    fileType = type; // if all the bytes match, return the type
                    break;
                }
            }
            return fileType;
        }

        /// <summary>
        /// Determines whether provided file belongs to one of the provided list of files
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="requiredTypes">The required types.</param>
        /// <returns>
        ///   <c>true</c> if file of the one of the provided types; otherwise, <c>false</c>.
        /// </returns>
        public static bool isFileOfTypes(this FileInfo file, List<FileType> requiredTypes)
        {
            FileType currentType = file.GetFileType();

            if (null == currentType)
            {
                return false;
            }

            return requiredTypes.Contains(currentType);
        }

        /// <summary>
        /// Determines whether provided file belongs to one of the provided list of files,
        /// where list of files provided by string with Comma-Separated-Values of extensions
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="requiredTypes">The required types.</param>
        /// <returns>
        ///   <c>true</c> if file of the one of the provided types; otherwise, <c>false</c>.
        /// </returns>
        public static bool isFileOfTypes(this FileInfo file, String CSV)
        {
            List<FileType> providedTypes = GetFileTypesByExtensions(CSV);

            return file.isFileOfTypes(providedTypes);
        }

        /// <summary>
        /// Gets the list of FileTypes based on list of extensions in Comma-Separated-Values string
        /// </summary>
        /// <param name="CSV">The CSV String with extensions</param>
        /// <returns>List of FileTypes</returns>
        private static List<FileType> GetFileTypesByExtensions(String CSV)
        {
            String[] extensions = CSV.ToUpper().Replace(" ", "").Split(',');

            List<FileType> result = new List<FileType>();

            foreach (FileType type in types)
            {
                if (extensions.Contains(type.Extension.ToUpper()))
                {
                    result.Add(type);
                }
            }
            return result;
        }

        private static int GetFileMatchingCount(byte[] fileHeader, FileType type)
        {
            int matchingCount = 0;
            for (int i = 0; i < type.Header.Length; i++)
            {
                // if file offset is not set to zero, we need to take this into account when comparing.
                // if byte in type.header is set to null, means this byte is variable, ignore it
                if (type.Header[i] != null && type.Header[i] != fileHeader[i + type.HeaderOffset])
                {
                    // if one of the bytes does not match, move on to the next type
                    matchingCount = 0;
                    break;
                }
                else
                {
                    matchingCount++;
                }
            }

            return matchingCount;
        }

        /// <summary>
        /// Reads the file header - first (16) bytes from the file
        /// </summary>
        /// <param name="file">The file to work with</param>
        /// <returns>Array of bytes</returns>
        private static Byte[] ReadFileHeader(FileInfo file, int MaxHeaderSize)
        {
            Byte[] header = new byte[MaxHeaderSize];
            try  // read file
            {
                using (FileStream fsSource = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                {
                    // read first symbols from file into array of bytes.
                    fsSource.Read(header, 0, MaxHeaderSize);
                }   // close the file stream

            }
            catch (Exception e) // file could not be found/read
            {
                throw new ApplicationException("Could not read file : " + e.Message);
            }

            return header;
        }
        #endregion

        #region isType functions


        /// <summary>
        /// Determines whether the specified file is of provided type
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="type">The FileType</param>
        /// <returns>
        ///   <c>true</c> if the specified file is type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsType(this FileInfo file, FileType type)
        {
            FileType actualType = GetFileType(file);

            if (null == actualType)
                return false;

            return (actualType.Equals(type));
        }
        #endregion
    }
}