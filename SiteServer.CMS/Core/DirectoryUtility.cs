﻿using System;
using SiteServer.Utils;
using SiteServer.CMS.Model;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SiteServer.CMS.Model.Enumerations;
using SiteServer.Plugin;

namespace SiteServer.CMS.Core
{
    public class DirectoryUtility
    {
        public static string GetIndexesDirectoryPath(string siteFilesDirectoryPath)
        {
            return PathUtils.Combine(siteFilesDirectoryPath, "Indexes");
        }

        public static void ChangeSiteDir(string parentPsPath, string oldPsDir, string newPsDir)
        {
            var oldPsPath = PathUtils.Combine(parentPsPath, oldPsDir);
            var newPsPath = PathUtils.Combine(parentPsPath, newPsDir);
            if (DirectoryUtils.IsDirectoryExists(newPsPath))
            {
                throw new ArgumentException("发布系统修改失败，发布路径文件夹已存在！");
            }
            if (DirectoryUtils.IsDirectoryExists(oldPsPath))
            {
                DirectoryUtils.MoveDirectory(oldPsPath, newPsPath, false);
            }
            else
            {
                DirectoryUtils.CreateDirectoryIfNotExists(newPsPath);
            }
        }

        public static void DeleteSiteFiles(SiteInfo siteInfo)
        {
            var sitePath = PathUtility.GetSitePath(siteInfo);

            if (siteInfo.IsRoot)
            {
                var filePaths = DirectoryUtils.GetFilePaths(sitePath);
                foreach (var filePath in filePaths)
                {
                    var fileName = PathUtils.GetFileName(filePath);
                    if (!PathUtility.IsSystemFile(fileName))
                    {
                        FileUtils.DeleteFileIfExists(filePath);
                    }
                }

                var siteDirList = DataProvider.SiteDao.GetLowerSiteDirListThatNotIsRoot();

                var directoryPaths = DirectoryUtils.GetDirectoryPaths(sitePath);
                foreach (var subDirectoryPath in directoryPaths)
                {
                    var directoryName = PathUtils.GetDirectoryName(subDirectoryPath);
                    if (!DirectoryUtils.IsSystemDirectory(directoryName) && !siteDirList.Contains(directoryName.ToLower()))
                    {
                        DirectoryUtils.DeleteDirectoryIfExists(subDirectoryPath);
                    }
                }
            }
            else
            {
                var direcotryPath = sitePath;
                DirectoryUtils.DeleteDirectoryIfExists(direcotryPath);
            }
        }

        public static void ImportSiteFiles(SiteInfo siteInfo, string siteTemplatePath, bool isOverride)
        {
            var sitePath = PathUtility.GetSitePath(siteInfo);

            if (siteInfo.IsRoot)
            {
                var filePaths = DirectoryUtils.GetFilePaths(siteTemplatePath);
                foreach (var filePath in filePaths)
                {
                    var fileName = PathUtils.GetFileName(filePath);
                    if (!PathUtility.IsSystemFile(fileName))
                    {
                        var destFilePath = PathUtils.Combine(sitePath, fileName);
                        FileUtils.MoveFile(filePath, destFilePath, isOverride);
                    }
                }

                var siteDirList = DataProvider.SiteDao.GetLowerSiteDirListThatNotIsRoot();

                var directoryPaths = DirectoryUtils.GetDirectoryPaths(siteTemplatePath);
                foreach (var subDirectoryPath in directoryPaths)
                {
                    var directoryName = PathUtils.GetDirectoryName(subDirectoryPath);
                    if (!DirectoryUtils.IsSystemDirectory(directoryName) && !siteDirList.Contains(directoryName.ToLower()))
                    {
                        var destDirectoryPath = PathUtils.Combine(sitePath, directoryName);
                        DirectoryUtils.MoveDirectory(subDirectoryPath, destDirectoryPath, isOverride);
                    }
                }
            }
            else
            {
                DirectoryUtils.MoveDirectory(siteTemplatePath, sitePath, isOverride);
            }
            var siteTemplateMetadataPath = PathUtils.Combine(sitePath, DirectoryUtils.SiteTemplates.SiteTemplateMetadata);
            DirectoryUtils.DeleteDirectoryIfExists(siteTemplateMetadataPath);
        }

        public static void ChangeParentSite(int oldParentSiteId, int newParentSiteId, int siteId, string siteDir)
        {
            if (oldParentSiteId == newParentSiteId) return;

            string oldPsPath;
            if (oldParentSiteId != 0)
            {
                var oldSiteInfo = SiteManager.GetSiteInfo(oldParentSiteId);

                oldPsPath = PathUtils.Combine(PathUtility.GetSitePath(oldSiteInfo), siteDir);
            }
            else
            {
                var siteInfo = SiteManager.GetSiteInfo(siteId);
                oldPsPath = PathUtility.GetSitePath(siteInfo);
            }

            string newPsPath;
            if (newParentSiteId != 0)
            {
                var newSiteInfo = SiteManager.GetSiteInfo(newParentSiteId);

                newPsPath = PathUtils.Combine(PathUtility.GetSitePath(newSiteInfo), siteDir);
            }
            else
            {
                newPsPath = PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, siteDir);
            }

            if (DirectoryUtils.IsDirectoryExists(newPsPath))
            {
                throw new ArgumentException("发布系统修改失败，发布路径文件夹已存在！");
            }
            if (DirectoryUtils.IsDirectoryExists(oldPsPath))
            {
                DirectoryUtils.MoveDirectory(oldPsPath, newPsPath, false);
            }
            else
            {
                DirectoryUtils.CreateDirectoryIfNotExists(newPsPath);
            }
        }

        public static void DeleteContentsByPage(SiteInfo siteInfo, List<int> nodeIdList)
        {
            foreach (var nodeId in nodeIdList)
            {
                var tableName = ChannelManager.GetTableName(siteInfo, nodeId);
                var contentIdList = DataProvider.ContentDao.GetContentIdList(tableName, nodeId);
                if (contentIdList.Count > 0)
                {
                    foreach (var contentId in contentIdList)
                    {
                        var filePath = PathUtility.GetContentPageFilePath(siteInfo, nodeId, contentId, 0);
                        FileUtils.DeleteFileIfExists(filePath);
                        DeletePagingFiles(filePath);
                        DirectoryUtils.DeleteEmptyDirectory(DirectoryUtils.GetDirectoryPath(filePath));
                    }
                }
            }
        }

        public static void DeleteContents(SiteInfo siteInfo, int nodeId, List<int> contentIdList)
        {
            foreach (var contentId in contentIdList)
            {
                var filePath = PathUtility.GetContentPageFilePath(siteInfo, nodeId, contentId, 0);
                FileUtils.DeleteFileIfExists(filePath);
            }
        }

        public static void DeleteChannels(SiteInfo siteInfo, List<int> nodeIdList)
        {
            foreach (var nodeId in nodeIdList)
            {
                var filePath = PathUtility.GetChannelPageFilePath(siteInfo, nodeId, 0);

                FileUtils.DeleteFileIfExists(filePath);

                var tableName = ChannelManager.GetTableName(siteInfo, nodeId);
                var contentIdList = DataProvider.ContentDao.GetContentIdList(tableName, nodeId);
                if (contentIdList.Count > 0)
                {
                    DeleteContents(siteInfo, nodeId, contentIdList);
                }
            }
        }

        public static void DeleteChannelsByPage(SiteInfo siteInfo, List<int> nodeIdList)
        {
            foreach (var nodeId in nodeIdList)
            {
                if (nodeId != siteInfo.Id)
                {
                    var filePath = PathUtility.GetChannelPageFilePath(siteInfo, nodeId, 0);
                    FileUtils.DeleteFileIfExists(filePath);
                    DeletePagingFiles(filePath);
                    DirectoryUtils.DeleteEmptyDirectory(DirectoryUtils.GetDirectoryPath(filePath));
                }
            }
        }

        public static void DeletePagingFiles(string filePath)
        {
            var fileName = (new FileInfo(filePath)).Name;
            fileName = fileName.Substring(0, fileName.IndexOf('.'));
            var filesPath = DirectoryUtils.GetFilePaths(DirectoryUtils.GetDirectoryPath(filePath));
            foreach (var otherFilePath in filesPath)
            {
                var otherFileName = (new FileInfo(otherFilePath)).Name;
                otherFileName = otherFileName.Substring(0, otherFileName.IndexOf('.'));
                if (otherFileName.Contains(fileName + "_"))
                {
                    var isNum = otherFileName.Replace(fileName + "_", string.Empty);
                    if (ConvertHelper.GetInteger(isNum) > 0)
                    {
                        FileUtils.DeleteFileIfExists(otherFilePath);
                    }
                }
            }
        }

        public static void DeleteFiles(SiteInfo siteInfo, List<int> templateIdList)
        {
            foreach (var templateId in templateIdList)
            {
                var templateInfo = TemplateManager.GetTemplateInfo(siteInfo.Id, templateId);
                if (templateInfo == null || templateInfo.TemplateType != TemplateType.FileTemplate)
                {
                    return;
                }

                var filePath = PathUtility.MapPath(siteInfo, templateInfo.CreatedFileFullName);

                FileUtils.DeleteFileIfExists(filePath);
            }
        }

        public static void ChangeToHeadquarters(SiteInfo siteInfo, bool isMoveFiles)
        {
            if (siteInfo.IsRoot == false)
            {
                var sitePath = PathUtility.GetSitePath(siteInfo);

                DataProvider.SiteDao.UpdateParentIdToZero(siteInfo.Id);

                siteInfo.IsRoot = true;
                siteInfo.SiteDir = string.Empty;

                DataProvider.SiteDao.Update(siteInfo);
                if (isMoveFiles)
                {
                    DirectoryUtils.MoveDirectory(sitePath, WebConfigUtils.PhysicalApplicationPath, false);
                    DirectoryUtils.DeleteDirectoryIfExists(sitePath);
                }
            }
        }

        public static void ChangeToSubSite(SiteInfo siteInfo, string psDir, ArrayList fileSystemNameArrayList)
        {
            if (siteInfo.IsRoot)
            {
                siteInfo.IsRoot = false;
                siteInfo.SiteDir = psDir.Trim();

                DataProvider.SiteDao.Update(siteInfo);

                var psPath = PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, psDir);
                DirectoryUtils.CreateDirectoryIfNotExists(psPath);
                if (fileSystemNameArrayList != null && fileSystemNameArrayList.Count > 0)
                {
                    foreach (string fileSystemName in fileSystemNameArrayList)
                    {
                        var srcPath = PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, fileSystemName);
                        if (DirectoryUtils.IsDirectoryExists(srcPath))
                        {
                            var destDirectoryPath = PathUtils.Combine(psPath, fileSystemName);
                            DirectoryUtils.CreateDirectoryIfNotExists(destDirectoryPath);
                            DirectoryUtils.MoveDirectory(srcPath, destDirectoryPath, false);
                            DirectoryUtils.DeleteDirectoryIfExists(srcPath);
                        }
                        else if (FileUtils.IsFileExists(srcPath))
                        {
                            FileUtils.CopyFile(srcPath, PathUtils.Combine(psPath, fileSystemName));
                            FileUtils.DeleteFileIfExists(srcPath);
                        }
                    }
                }
            }
        }
    }
}
