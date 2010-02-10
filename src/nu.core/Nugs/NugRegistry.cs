// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace nu.core.Nugs
{
    using System;
    using System.IO;
    using Configuration;
    using FileSystem;
    using Model.Files.Package;
    using NDepend.Helpers.FileDirectoryPath;

    public class NugRegistry
    {
        readonly FileSystem _fileSystem;

        public NugRegistry(FileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public FilePathAbsolute GetNug(string nugName)
        {
            var x = _fileSystem.NugsDirectory;
            var n = x.GetChildFileWithName(nugName);
            return n;
        }

        public NugPackage GetNugPackage(string name)
        {
            var path = _fileSystem.NugsDirectory.GetChildFileWithName(string.Format("{0}.nug", name));
            var target = new DirectoryPathAbsolute("");
            Zip.Unzip(path, target);
            var manifest = target.GetChildFileWithName("MANIFEST");
            var manifestContent = System.IO.File.ReadAllText(manifest.Path);
            var m = JsonUtil.Get<Manifest>(manifestContent);
            var np = new NugPackage(m.Name);
            np.Version = m.Version;

            foreach (var entry in m.Files)
            {
                //get the file
                //load into a stream
                np.Files.Add(new NugFile()
                    {
                        Name = entry.Name,
                        File = null //the file stream
                    });
            }

            return np;
        }

        public NugPackage Do(string nugName)
        {
            var nug = new NugPackage(nugName);
            _fileSystem.WorkWithTempDir((temp) =>
                {
                    var n = GetNug(nugName);

                    if (!n.Exists)
                        throw new Exception("cant find nug");

                    Zip.Unzip(n, temp);

                    //i now have the unzipped contents @ temp
                    var mani = temp.GetChildFileWithName("MANIFEST");
                    var maniS = System.IO.File.ReadAllText(mani.Path);
                    //json it
                    var m = JsonUtil.Get<Manifest>(maniS);

                    nug.Name = m.Name;
                    nug.Version = m.Version;

                    foreach (var entry in m.Files)
                    {
                        nug.Files.Add(new NugFile
                            {
                                Name = entry.Name,
                                //whoa
                                File = new MemoryStream(System.IO.File.ReadAllBytes(mani.GetBrotherFileWithName(entry.Name).Path))
                            });
                    }
                });
            return nug;
        }
    }
}