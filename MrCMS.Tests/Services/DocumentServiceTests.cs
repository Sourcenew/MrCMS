﻿using System;
using System.Linq;
using Iesi.Collections.Generic;
using MrCMS.Entities.Documents;
using MrCMS.Entities.Documents.Layout;
using MrCMS.Entities.Documents.Web;
using MrCMS.Entities.Multisite;
using MrCMS.Entities.Widget;
using MrCMS.Helpers;
using MrCMS.Services;
using MrCMS.Settings;
using MrCMS.Tests.Stubs;
using MrCMS.Website;
using Xunit;
using FluentAssertions;

namespace MrCMS.Tests.Services
{
    public class DocumentServiceTests : InMemoryDatabaseTest
    {
        private readonly SiteSettings _siteSettings;
        private readonly DocumentService _documentService;

        public DocumentServiceTests()
        {
            _documentService = new DocumentService(Session,  _siteSettings, CurrentSite);
            _siteSettings = new SiteSettings();
        }

        [Fact]
        public void AddDocument_OnSave_AddsToRepository()
        {
            _documentService.AddDocument(new BasicMappedWebpage { Site = CurrentSite });

            Session.QueryOver<Document>().RowCount().Should().Be(1);
        }


        [Fact]
        public void GetDocument_WhenDocumentDoesNotExist_ReturnsNull()
        {
            var document = _documentService.GetDocument<BasicMappedWebpage>(1);

            document.Should().BeNull();
        }

        [Fact]
        public void DocumentService_SaveDocument_ReturnsTheSameDocument()
        {
            var document = new BasicMappedWebpage();
            Session.Transact(session => session.Save(document));
            
            var updatedDocument = _documentService.SaveDocument(document);

            document.Should().BeSameAs(updatedDocument);
        }

        [Fact]
        public void DocumentService_GetAllDocuments_ShouldReturnAListOfAllDocumentsOfTheSpecifiedType()
        {
            Enumerable.Range(1, 10).ForEach(i => Session.Transact(session => session.SaveOrUpdate(new BasicMappedWebpage { Name = "Page " + i })));

            var allDocuments = _documentService.GetAllDocuments<BasicMappedWebpage>();

            allDocuments.Should().HaveCount(10);
        }

        [Fact]
        public void DocumentService_GetAllDocuments_ShouldOnlyReturnDocumentsOfSpecifiedType()
        {
            Enumerable.Range(1, 10).ForEach(i =>
                                         Session.Transact(
                                             session =>
                                             session.SaveOrUpdate(i % 2 == 0
                                                                      ? (Document)new BasicMappedWebpage { Name = "Page " + i }
                                                                      : new Layout { Name = "Layout " + i }
                                                 )));

            var allDocuments = _documentService.GetAllDocuments<BasicMappedWebpage>();

            allDocuments.Should().HaveCount(5);
        }


        [Fact]
        public void DocumentService_GetDocumentByUrl_ReturnsTheDocumentWithTheSpecifiedUrl()
        {
            var textPage = new BasicMappedWebpage { UrlSegment = "test-page", Site = CurrentSite };
            Session.Transact(session => session.SaveOrUpdate(textPage));

            var document = _documentService.GetDocumentByUrl<BasicMappedWebpage>("test-page");

            document.Should().NotBeNull();
        }

        [Fact]
        public void DocumentService_GetDocumentByUrl_ShouldReturnNullIfTheRequestedTypeDoesNotMatch()
        {
            Site site = new Site();
            var textPage = new BasicMappedWebpage { UrlSegment = "test-page", Site = site };
            Session.Transact(session => session.SaveOrUpdate(textPage));

            var document = _documentService.GetDocumentByUrl<Layout>("test-page");

            document.Should().BeNull();
        }
        [Fact]
        public void DocumentService_SetTags_IfDocumentIsNullThrowArgumentNullException()
        {
            _documentService.Invoking(service => service.SetTags(null, null)).ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void DocumentService_SetTags_IfTagsIsNullForANewDocumentTheTagListShouldBeEmpty()
        {
            var textPage = new BasicMappedWebpage();

            _documentService.SetTags(null, textPage);

            textPage.Tags.Should().HaveCount(0);
        }

        [Fact]
        public void DocumentService_SetTags_IfTagsHasOneStringTheTagListShouldHave1Tag()
        {
            var textPage = new BasicMappedWebpage();

            _documentService.SetTags("test tag", textPage);

            textPage.Tags.Should().HaveCount(1);
        }

        [Fact]
        public void DocumentService_SetTags_IfTagsHasTwoCommaSeparatedTagsTheTagListShouldHave2Tags()
        {
            var textPage = new BasicMappedWebpage();

            _documentService.SetTags("test 1, test 2", textPage);

            textPage.Tags.Should().HaveCount(2);
        }

        [Fact]
        public void DocumentService_SetTags_ShouldTrimTagNames()
        {
            var textPage = new BasicMappedWebpage();

            _documentService.SetTags("test 1, test 2", textPage);

            textPage.Tags.ElementAt(1).Name.Should().Be("test 2");
        }

        [Fact]
        public void DocumentService_SetTags_ShouldAddTagsToDocument()
        {
            var textPage = new BasicMappedWebpage();

            _documentService.SetTags("test 1, test 2", textPage);

            textPage.Tags.Should().HaveCount(2);
        }

        [Fact]
        public void DocumentService_SetTags_ShouldNotRecreateTags()
        {
            var textPage = new BasicMappedWebpage();
            var tag1 = new Tag { Name = "test 1" };
            var tag2 = new Tag { Name = "test 2" };
            textPage.Tags.Add(tag1);
            textPage.Tags.Add(tag2);

            Session.Transact(session =>
                                 {
                                     session.SaveOrUpdate(textPage);
                                     session.SaveOrUpdate(tag1);
                                     session.SaveOrUpdate(tag2);
                                 });

            _documentService.SetTags(textPage.TagList, textPage);

            Session.QueryOver<Tag>().RowCount().Should().Be(2);
        }
        [Fact]
        public void DocumentService_SetTags_ShouldNotReaddSetTags()
        {
            var textPage = new BasicMappedWebpage();
            var tag1 = new Tag { Name = "test 1" };
            var tag2 = new Tag { Name = "test 2" };
            textPage.Tags.Add(tag1);
            textPage.Tags.Add(tag2);

            Session.Transact(session =>
            {
                session.SaveOrUpdate(textPage);
                session.SaveOrUpdate(tag1);
                session.SaveOrUpdate(tag2);
            });

            _documentService.SetTags(textPage.TagList, textPage);

            textPage.Tags.Should().HaveCount(2);
        }

        [Fact]
        public void DocumentService_SetTags_ShouldRemoveTagsNotIncluded()
        {
            var textPage = new BasicMappedWebpage();
            var tag1 = new Tag { Name = "test 1" };
            var tag2 = new Tag { Name = "test 2" };
            textPage.Tags.Add(tag1);
            textPage.Tags.Add(tag2);

            Session.Transact(session =>
                                 {
                                     session.SaveOrUpdate(textPage);
                                     session.SaveOrUpdate(tag1);
                                     session.SaveOrUpdate(tag2);
                                 });

            _documentService.SetTags("test 1", textPage);

            textPage.Tags.Should().HaveCount(1);
        }

        [Fact]
        public void DocumentService_SetTags_ShouldAssignDocumentToTag()
        {
            var textPage = new BasicMappedWebpage();
            Session.Transact(session => session.SaveOrUpdate(textPage));

            _documentService.SetTags("test 1", textPage);

            var tags = textPage.Tags;
            tags.Should().HaveCount(1);
            tags.First().Documents.Should().HaveCount(1);
        }

        [Fact]
        public void DocumentService_SetTags_ShouldRemoveTheDocumentFromTags()
        {
            var textPage = new BasicMappedWebpage();
            var tag1 = new Tag { Name = "test 1" };
            var tag2 = new Tag { Name = "test 2" };
            textPage.Tags.Add(tag1);
            textPage.Tags.Add(tag2);
            tag1.Documents.Add(textPage);
            tag2.Documents.Add(textPage);

            Session.Transact(session =>
                                 {
                                     session.SaveOrUpdate(textPage);
                                     session.SaveOrUpdate(tag1);
                                     session.SaveOrUpdate(tag2);
                                 });

            _documentService.SetTags("test 1", textPage);

            tag1.Documents.Should().HaveCount(1);
            tag2.Documents.Should().HaveCount(0);
        }

        [Fact]
        public void DocumentService_SetTags_ShouldNotCreateTagsWithEmptyNames()
        {
            var textPage = new BasicMappedWebpage();

            _documentService.SetTags("test 1,,test 2", textPage);

            textPage.Tags.Should().HaveCount(2);
        }

        [Fact]
        public void DocumentService_SetTags_ShouldNotCreateTagsWithEmptyNamesForTrailingComma()
        {
            var textPage = new BasicMappedWebpage();

            _documentService.SetTags("test 1, test 2, ", textPage);

            textPage.Tags.Should().HaveCount(2);
        }

        [Fact]
        public void DocumentService_SetOrder_ShouldSetTheDocumentOrderOfTheDocumentWithTheSetId()
        {
            var textPage = new BasicMappedWebpage();

            Session.Transact(session => session.SaveOrUpdate(textPage));

            _documentService.SetOrder(textPage.Id, 2);

            textPage.DisplayOrder.Should().Be(2);
        }

        [Fact]
        public void DocumentService_AnyWebpages_ReturnsFalseWhenNoWebpagesAreSaved()
        {
            _documentService.AnyWebpages().Should().BeFalse();
        }

        [Fact]
        public void DocumentService_AnyWebpages_ReturnsTrueOnceAWebpageIsAdded()
        {
            _documentService.AddDocument(new BasicMappedWebpage { Site = CurrentSite });

            _documentService.AnyWebpages().Should().BeTrue();
        }

        [Fact]
        public void DocumentService_AnyPublishedWebpages_ReturnsFalseWhenThereAreNoWebpages()
        {
            _documentService.AnyPublishedWebpages().Should().BeFalse();
        }

        [Fact]
        public void DocumentService_AnyPublishedWebpages_ReturnsFalseWhenThereAreWebpagesButTheyAreNotPublished()
        {
            _documentService.AddDocument(new BasicMappedWebpage() { Site = CurrentSite });

            _documentService.AnyPublishedWebpages().Should().BeFalse();
        }

        [Fact]
        public void DocumentService_AnyPublishedWebpages_ReturnsTrueOnceAPublishedWebpageIsAdded()
        {
            _documentService.AddDocument(new BasicMappedWebpage { Site = CurrentSite, PublishOn = CurrentRequestData.Now.AddDays(-1) });

            _documentService.AnyPublishedWebpages().Should().BeTrue();
        }

        [Fact]
        public void DocumentService_HideWidget_AddsAWidgetToTheHiddenWidgetsListIfItIsNotInTheShownList()
        {
            var widgetService = new WidgetService(Session);

            var textPage = new BasicMappedWebpage { ShownWidgets = new HashedSet<Widget>(), HiddenWidgets = new HashedSet<Widget>() };
            _documentService.AddDocument(textPage);

            var textWidget = new BasicMappedWidget();
            widgetService.SaveWidget(textWidget);

            _documentService.HideWidget(textPage, textWidget.Id);

            textPage.HiddenWidgets.Should().Contain(textWidget);
        }

        [Fact]
        public void DocumentService_HideWidget_RemovesAWidgetFromTheShownListIfItIsIncluded()
        {
            var widgetService = new WidgetService(Session);

            var textWidget = new BasicMappedWidget();
            widgetService.SaveWidget(textWidget);

            var textPage = new BasicMappedWebpage
            {
                ShownWidgets = new HashedSet<Widget> { textWidget },
                HiddenWidgets = new HashedSet<Widget>()
            };
            _documentService.AddDocument(textPage);

            _documentService.HideWidget(textPage, textWidget.Id);

            textPage.ShownWidgets.Should().NotContain(textWidget);
        }

        [Fact]
        public void DocumentService_HideWidget_DoesNothingIfTheWidgetIdIsInvalid()
        {
            var widgetService = new WidgetService(Session);

            var textWidget = new BasicMappedWidget();
            widgetService.SaveWidget(textWidget);

            var textPage = new BasicMappedWebpage
            {
                ShownWidgets = new HashedSet<Widget> { textWidget },
                HiddenWidgets = new HashedSet<Widget>()
            };
            _documentService.AddDocument(textPage);

            _documentService.HideWidget(textPage, -1);

            textPage.ShownWidgets.Should().Contain(textWidget);
        }


        [Fact]
        public void DocumentService_ShowWidget_AddsAWidgetToTheShownWidgetsListIfItIsNotInTheHiddenList()
        {
            var widgetService = new WidgetService(Session);

            var textPage = new BasicMappedWebpage { ShownWidgets = new HashedSet<Widget>(), HiddenWidgets = new HashedSet<Widget>() };
            _documentService.AddDocument(textPage);

            var textWidget = new BasicMappedWidget();
            widgetService.SaveWidget(textWidget);

            _documentService.ShowWidget(textPage, textWidget.Id);

            textPage.ShownWidgets.Should().Contain(textWidget);
        }

        [Fact]
        public void DocumentService_ShowWidget_RemovesAWidgetFromTheHiddenListIfItIsIncluded()
        {
            var widgetService = new WidgetService(Session);

            var textWidget = new BasicMappedWidget();
            widgetService.SaveWidget(textWidget);

            var textPage = new BasicMappedWebpage
            {
                ShownWidgets = new HashedSet<Widget>(),
                HiddenWidgets = new HashedSet<Widget> { textWidget }
            };
            _documentService.AddDocument(textPage);

            _documentService.ShowWidget(textPage, textWidget.Id);

            textPage.HiddenWidgets.Should().NotContain(textWidget);
        }

        [Fact]
        public void DocumentService_ShowWidget_DoesNothingIfTheWidgetIdIsInvalid()
        {
            
            var widgetService = new WidgetService(Session);

            var textWidget = new BasicMappedWidget();
            widgetService.SaveWidget(textWidget);

            var textPage = new BasicMappedWebpage
            {
                ShownWidgets = new HashedSet<Widget>(),
                HiddenWidgets = new HashedSet<Widget> { textWidget }
            };
            _documentService.AddDocument(textPage);

            _documentService.ShowWidget(textPage, -1);

            textPage.HiddenWidgets.Should().Contain(textWidget);
        }

        [Fact]
        public void DocumentService_PublishNow_UnpublishedWebpageWillGetPublishedOnValue()
        {
            

            var textPage = new BasicMappedWebpage();

            Session.Transact(session => session.Save(textPage));

            _documentService.PublishNow(textPage);

            textPage.PublishOn.Should().HaveValue();
        }

        [Fact]
        public void DocumentService_PublishNow_PublishedWebpageShouldNotChangeValue()
        {
            

            var publishOn = CurrentRequestData.Now.AddDays(-1);
            var textPage = new BasicMappedWebpage { PublishOn = publishOn };

            Session.Transact(session => session.Save(textPage));

            _documentService.PublishNow(textPage);

            textPage.PublishOn.Should().Be(publishOn);
        }


        [Fact]
        public void DocumentService_Unpublish_ShouldSetPublishOnToNull()
        {
            var publishOn = CurrentRequestData.Now.AddDays(-1);
            var textPage = new BasicMappedWebpage { PublishOn = publishOn };

            Session.Transact(session => session.Save(textPage));

            _documentService.Unpublish(textPage);

            textPage.PublishOn.Should().NotHaveValue();
        }

        [Fact]
        public void DocumentService_DeleteDocument_ShouldCallSessionDelete()
        {
            var textPage = new BasicMappedWebpage();
            Session.Transact(session => session.Save(textPage));

            _documentService.DeleteDocument(textPage);

            Session.QueryOver<Webpage>().RowCount().Should().Be(0);
        }

        [Fact]
        public void DocumentService_GetDocumentVersion_GetsTheVersionWithTheRequestedId()
        {
            var documentVersion = new DocumentVersion();
            Session.Transact(session => session.Save(documentVersion));

            var version = _documentService.GetDocumentVersion(documentVersion.Id);

            version.Should().Be(documentVersion);
        }

        [Fact]
        public void DocumentService_AddDocument_RootDocShouldSetDisplayOrderToMaxOfNonParentDocsPlus1()
        {
            for (int i = 0; i < 4; i++)
            {
                Session.Transact(session => session.Save(new StubWebpage { DisplayOrder = i, Site = CurrentSite }));
            }

            var stubDocument = new StubWebpage { Site = CurrentSite };
            _documentService.AddDocument(stubDocument);

            stubDocument.DisplayOrder.Should().Be(4);
        }
    }
}
