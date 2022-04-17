using LanZou;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace Test
{
    public static class AConfig
    {
        public const string TestFolder = "LanZouApiTestFolder";

        public const string RootPath = "../../../../../../LanZouTest/";
        public const string TestFilePath = RootPath + "LanZouApiTestFile.txt";
        public const string TestFileBigPath = RootPath + "LanZouApiTestFileBig.w3x";
        public const string cookieFilePath = RootPath + "cookie.txt";


        public static LanZouCloud Cloud()
        {
            var cloud = new LanZouCloud();
            return cloud;
        }

        public static async Task<LanZouCloud> AsyncLoginCloud(bool validate = false)
        {
            var cloud = new LanZouCloud();
            string[] cookies = File.ReadAllText(cookieFilePath).Split(',');
            var login = await cloud.Login(cookies[0], cookies[1], validate);
            Assert.IsTrue(login.code == ResultCode.SUCCESS);
            return cloud;
        }

        public static async Task<long> GetTestFolder(LanZouCloud cloud, string name = null, long parent_id = -1)
        {
            name = name ?? TestFolder;
            long folderId = 0;
            var fileList = await cloud.GetFolderList(parent_id);
            Assert.IsTrue(fileList.code == ResultCode.SUCCESS);
            var folder = fileList.folders.Find(a => a.name == name);
            if (folder == null)
            {
                var create = await cloud.CreateFolder(name, parent_id);
                Assert.IsTrue(create.code == ResultCode.SUCCESS);
                folderId = create.id;
            }
            else
            {
                folderId = folder.id;
            }
            return folderId;
        }

        public static async Task<long> GetTestFileBig(LanZouCloud cloud)
        {
            return await GetTestFile(cloud, TestFileBigPath);
        }

        public static async Task<long> GetTestFile(LanZouCloud cloud)
        {
            return await GetTestFile(cloud, TestFilePath);
        }

        private static async Task<long> GetTestFile(LanZouCloud cloud, string filepath)
        {
            long fileId = 0;
            var fileList = await cloud.GetFileList();
            Assert.IsTrue(fileList.code == ResultCode.SUCCESS);
            var file = fileList.files.Find(a => a.name == Path.GetFileName(filepath));
            if (file == null)
            {
                var create = await cloud.UploadFile(filepath);
                Assert.IsTrue(create.code == ResultCode.SUCCESS);
                fileId = create.id;
            }
            else
            {
                fileId = file.id;
            }
            return fileId;
        }

    }
}
