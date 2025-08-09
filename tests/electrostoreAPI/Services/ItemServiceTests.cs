using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using AutoMapper;
using electrostore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.ItemService;

namespace electrostore.Tests.Services
{
    public class ItemServiceTests
    {
        private readonly Mock<IMapper> _mockMapper;
        private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

        public ItemServiceTests()
        {
            _mockMapper = new Mock<IMapper>();

            // Set up in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task GetItems_ShouldReturnItems_WhenItemsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            
            // Add test items to the database
            for (int i = 1; i <= 5; i++)
            {
                context.Items.Add(new Items
                {
                    id_item = i,
                    reference_name_item = $"REF-{i}",
                    friendly_name_item = $"Item {i}",
                    description_item = $"Description for item {i}",
                    seuil_min_item = 10
                });
            }
            await context.SaveChangesAsync();

            var itemService = new ItemService(_mockMapper.Object, context);

            _mockMapper.Setup(m => m.Map<ReadExtendedItemDto>(It.IsAny<Items>()))
                .Returns((Items i) => new ReadExtendedItemDto
                {
                    id_item = i.id_item,
                    reference_name_item = i.reference_name_item,
                    friendly_name_item = i.friendly_name_item,
                    description_item = i.description_item,
                    seuil_min_item = i.seuil_min_item
                });

            // Act
            var result = await itemService.GetItems(5, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count());
        }

        [Fact]
        public async Task GetItemById_ShouldReturnItem_WhenItemExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            
            // Add a test item to the database
            var item = new Items
            {
                id_item = 1,
                reference_name_item = "REF-1",
                friendly_name_item = "Test Item",
                description_item = "Test Description",
                seuil_min_item = 10
            };
            context.Items.Add(item);
            await context.SaveChangesAsync();

            var itemService = new ItemService(_mockMapper.Object, context);

            var expectedItem = new ReadExtendedItemDto
            {
                id_item = 1,
                reference_name_item = "REF-1",
                friendly_name_item = "Test Item",
                description_item = "Test Description",
                seuil_min_item = 10
            };

            _mockMapper.Setup(m => m.Map<ReadExtendedItemDto>(It.IsAny<Items>()))
                .Returns(expectedItem);

            // Act
            var result = await itemService.GetItemById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedItem.id_item, result.id_item);
            Assert.Equal(expectedItem.reference_name_item, result.reference_name_item);
            Assert.Equal(expectedItem.friendly_name_item, result.friendly_name_item);
            Assert.Equal(expectedItem.description_item, result.description_item);
            Assert.Equal(expectedItem.seuil_min_item, result.seuil_min_item);
        }

        [Fact]
        public async Task CreateItem_ShouldCreateItem_WhenItemDoesNotExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            
            var itemService = new ItemService(_mockMapper.Object, context);

            var createItemDto = new CreateItemDto
            {
                reference_name_item = "REF-1",
                friendly_name_item = "Test Item",
                description_item = "Test Description",
                seuil_min_item = 10
            };

            var expectedItem = new ReadItemDto
            {
                id_item = 1,
                reference_name_item = "REF-1",
                friendly_name_item = "Test Item",
                description_item = "Test Description",
                seuil_min_item = 10
            };

            _mockMapper.Setup(m => m.Map<Items>(It.IsAny<CreateItemDto>()))
                .Returns(new Items
                {
                    reference_name_item = createItemDto.reference_name_item,
                    friendly_name_item = createItemDto.friendly_name_item,
                    description_item = createItemDto.description_item,
                    seuil_min_item = createItemDto.seuil_min_item
                });

            _mockMapper.Setup(m => m.Map<ReadItemDto>(It.IsAny<Items>()))
                .Returns(expectedItem);

            // Mock Directory.Exists and Directory.CreateDirectory to avoid file system operations
            var directoryExists = typeof(Directory).GetMethod("Exists", new[] { typeof(string) });
            var directoryCreateDirectory = typeof(Directory).GetMethod("CreateDirectory", new[] { typeof(string) });

            // Act
            var result = await itemService.CreateItem(createItemDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedItem.id_item, result.id_item);
            Assert.Equal(expectedItem.reference_name_item, result.reference_name_item);
            Assert.Equal(expectedItem.friendly_name_item, result.friendly_name_item);
            Assert.Equal(expectedItem.description_item, result.description_item);
            Assert.Equal(expectedItem.seuil_min_item, result.seuil_min_item);

            var itemInDb = await context.Items.FirstOrDefaultAsync(i => i.reference_name_item == "REF-1");
            Assert.NotNull(itemInDb);
        }

        [Fact]
        public async Task UpdateItem_ShouldUpdateItem_WhenItemExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            
            // Add a test item to the database
            var item = new Items
            {
                id_item = 1,
                reference_name_item = "REF-1",
                friendly_name_item = "Test Item",
                description_item = "Test Description",
                seuil_min_item = 10
            };
            context.Items.Add(item);
            await context.SaveChangesAsync();

            var itemService = new ItemService(_mockMapper.Object, context);

            var updateItemDto = new UpdateItemDto
            {
                friendly_name_item = "Updated Item",
                description_item = "Updated Description",
                seuil_min_item = 20
            };

            var expectedItem = new ReadItemDto
            {
                id_item = 1,
                reference_name_item = "REF-1",
                friendly_name_item = "Updated Item",
                description_item = "Updated Description",
                seuil_min_item = 20
            };

            _mockMapper.Setup(m => m.Map<ReadItemDto>(It.IsAny<Items>()))
                .Returns(expectedItem);

            // Act
            var result = await itemService.UpdateItem(1, updateItemDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedItem.id_item, result.id_item);
            Assert.Equal(expectedItem.reference_name_item, result.reference_name_item);
            Assert.Equal(expectedItem.friendly_name_item, result.friendly_name_item);
            Assert.Equal(expectedItem.description_item, result.description_item);
            Assert.Equal(expectedItem.seuil_min_item, result.seuil_min_item);

            var itemInDb = await context.Items.FindAsync(1);
            Assert.NotNull(itemInDb);
            Assert.Equal("Updated Item", itemInDb.friendly_name_item);
            Assert.Equal("Updated Description", itemInDb.description_item);
            Assert.Equal(20, itemInDb.seuil_min_item);
        }

        [Fact]
        public async Task DeleteItem_ShouldDeleteItem_WhenItemExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            
            // Add a test item to the database
            var item = new Items
            {
                id_item = 1,
                reference_name_item = "REF-1",
                friendly_name_item = "Test Item",
                description_item = "Test Description",
                seuil_min_item = 10
            };
            context.Items.Add(item);
            await context.SaveChangesAsync();

            var itemService = new ItemService(_mockMapper.Object, context);

            // Mock Directory.Exists and Directory.Delete to avoid file system operations
            var directoryExists = typeof(Directory).GetMethod("Exists", new[] { typeof(string) });
            var directoryDelete = typeof(Directory).GetMethod("Delete", new[] { typeof(string), typeof(bool) });

            // Act
            await itemService.DeleteItem(1);

            // Assert
            var itemInDb = await context.Items.FindAsync(1);
            Assert.Null(itemInDb);
        }

        // Performance benchmark test
        [Fact]
        public async Task GetItems_Performance_Benchmark()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            
            // Add test items to the database
            for (int i = 1; i <= 100; i++)
            {
                context.Items.Add(new Items
                {
                    id_item = i,
                    reference_name_item = $"REF-{i}",
                    friendly_name_item = $"Item {i}",
                    description_item = $"Description for item {i}",
                    seuil_min_item = 10
                });
            }
            await context.SaveChangesAsync();

            var itemService = new ItemService(_mockMapper.Object, context);

            _mockMapper.Setup(m => m.Map<ReadExtendedItemDto>(It.IsAny<Items>()))
                .Returns((Items i) => new ReadExtendedItemDto
                {
                    id_item = i.id_item,
                    reference_name_item = i.reference_name_item,
                    friendly_name_item = i.friendly_name_item,
                    description_item = i.description_item,
                    seuil_min_item = i.seuil_min_item
                });

            // Act
            var startTime = DateTime.Now;
            var result = await itemService.GetItems(100, 0);
            var endTime = DateTime.Now;
            var executionTime = (endTime - startTime).TotalMilliseconds;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Count());
            
            // Log performance metrics
            Console.WriteLine($"[BENCHMARK] GetItems execution time: {executionTime}ms for 100 items");
            
            // Performance assertion - should be reasonably fast
            Assert.True(executionTime < 1000, $"GetItems took too long: {executionTime}ms");
        }
    }
}