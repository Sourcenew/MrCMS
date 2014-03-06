﻿using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MrCMS.Entities.Documents.Media;
using MrCMS.Models;
using MrCMS.Services;
using MrCMS.Website.Controllers;

namespace MrCMS.Web.Areas.Admin.Controllers
{
    public class FileController : MrCMSAdminController
    {
        private readonly IFileService _fileService;

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpGet]
        public JsonResult Files(MediaCategory mediaCategory)
        {
            return Json(_fileService.GetFiles(mediaCategory), "text/html", System.Text.Encoding.UTF8);
        }


        [HttpPost]
        [ActionName("Files")]
        public JsonResult Files_Post(MediaCategory mediaCategory)
        {
            var list = new List<ViewDataUploadFilesResult>();
            foreach (string files in Request.Files)
            {
                var file = Request.Files[files];
                if (_fileService.IsValidFileType(file.FileName))
                {
                    var dbFile = _fileService.AddFile(file.InputStream, file.FileName,
                        file.ContentType, file.ContentLength,
                        mediaCategory);
                    list.Add(dbFile);
                }
            }
            return Json(list.ToArray(), "text/html", System.Text.Encoding.UTF8);
        }

        [HttpPost]
        [ActionName("Delete")]
        public ActionResult Delete_POST(MediaFile file)
        {
            var categoryId = file.MediaCategory.Id;
            _fileService.DeleteFile(file);
            return RedirectToAction("Show", "MediaCategory", new { Id = categoryId });
        }

        [HttpGet]
        public ActionResult Delete(MediaFile file)
        {
            return View("Delete", file);
        }

        [HttpPost]
        public string UpdateSEO(MediaFile mediaFile, string title, string description)
        {
            try
            {
                mediaFile.Title = title;
                mediaFile.Description = description;
                _fileService.SaveFile(mediaFile);

                return "Changes saved";
            }
            catch (Exception ex)
            {
                return string.Format("There was an error saving the SEO values: {0}", ex.Message);
            }
        }

        public ActionResult Edit(MediaFile file)
        {
            return View("Edit", file);
        }

        [HttpPost]
        [ActionName("Edit")]
        public ActionResult Edit_POST(MediaFile file)
        {
            _fileService.SaveFile(file);

            return RedirectToAction("Show", "MediaCategory", new { file.MediaCategory.Id });
        }
    }
}