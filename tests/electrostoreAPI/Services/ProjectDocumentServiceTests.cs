using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ProjectDocumentService;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjectDocumentServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService = new();

        private ProjectDocumentService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _fileService.Object);

        private static Projects BuildProject(string name = "project") => new()
        {
            name_project = name
        };

        private static Mock<IFormFile> BuildFormFile(string fileName = "doc.pdf", string contentType = "application/pdf", long length = 1024)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.Length).Returns(length);
            return file;
        }

        // --- GetProjectDocumentsByProjectId ---

        [Fact]
        public async Task GetProjectDocumentsByProjectId_ShouldReturnDocumentsForProject()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            context.ProjectsDocuments.Add(new ProjectsDocuments
            {
                id_project = project.id_project,
                url_project_document = "path/doc.pdf",
                name_project_document = "doc",
                type_project_document = "application/pdf",
                size_project_document = 100
            });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectDocumentsByProjectId(project.id_project);

            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetProjectDocumentsByProjectId_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectDocumentsByProjectId(999));
        }

        // --- GetProjectDocumentById ---

        [Fact]
        public async Task GetProjectDocumentById_ShouldReturnDocument_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var document = new ProjectsDocuments
            {
                id_project = project.id_project,
                url_project_document = "path/doc.pdf",
                name_project_document = "doc",
                type_project_document = "application/pdf",
                size_project_document = 100
            };
            context.ProjectsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectDocumentById(document.id_project_document);

            Assert.Equal(document.id_project_document, result.id_project_document);
        }

        [Fact]
        public async Task GetProjectDocumentById_ShouldThrowKeyNotFoundException_WhenProjectIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var document = new ProjectsDocuments
            {
                id_project = project.id_project,
                url_project_document = "path/doc.pdf",
                name_project_document = "doc",
                type_project_document = "application/pdf",
                size_project_document = 100
            };
            context.ProjectsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectDocumentById(document.id_project_document, project.id_project + 1));
        }

        [Fact]
        public async Task GetProjectDocumentById_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectDocumentById(999));
        }

        // --- CreateProjectDocument ---

        [Fact]
        public async Task CreateProjectDocument_ShouldPersistDocument_WhenProjectExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            _fileService.Setup(f => f.SaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = "projectDocuments/1/doc.pdf", mime_type = "application/pdf" });
            var service = CreateService(context);
            var dto = new CreateProjectDocumentDto { id_project = project.id_project, name_project_document = "doc", document = BuildFormFile().Object };

            var result = await service.CreateProjectDocument(dto);

            Assert.Equal("doc", result.name_project_document);
            Assert.Equal("projectDocuments/1/doc.pdf", result.url_project_document);
            Assert.Equal(1, await context.ProjectsDocuments.CountAsync());
        }

        [Fact]
        public async Task CreateProjectDocument_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateProjectDocumentDto { id_project = 999, name_project_document = "doc", document = BuildFormFile().Object };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateProjectDocument(dto));
        }

        // --- UpdateProjectDocument ---

        [Fact]
        public async Task UpdateProjectDocument_ShouldUpdateName()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var document = new ProjectsDocuments
            {
                id_project = project.id_project,
                url_project_document = "path/doc.pdf",
                name_project_document = "doc",
                type_project_document = "application/pdf",
                size_project_document = 100
            };
            context.ProjectsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateProjectDocumentDto { name_project_document = "renamed" };

            var result = await service.UpdateProjectDocument(document.id_project_document, dto);

            Assert.Equal("renamed", result.name_project_document);
        }

        [Fact]
        public async Task UpdateProjectDocument_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateProjectDocument(999, new UpdateProjectDocumentDto()));
        }

        // --- DeleteProjectDocument ---

        [Fact]
        public async Task DeleteProjectDocument_ShouldRemoveDocument_AndDeleteFile()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var document = new ProjectsDocuments
            {
                id_project = project.id_project,
                url_project_document = "path/doc.pdf",
                name_project_document = "doc",
                type_project_document = "application/pdf",
                size_project_document = 100
            };
            context.ProjectsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteProjectDocument(document.id_project_document);

            Assert.Equal(0, await context.ProjectsDocuments.CountAsync());
            _fileService.Verify(f => f.DeleteFile("path/doc.pdf"), Times.Once);
        }

        [Fact]
        public async Task DeleteProjectDocument_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteProjectDocument(999));
        }
    }
}
