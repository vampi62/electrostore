using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.ItemService;
using electrostore.Services.FileService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class ItemServiceTests : TestBase
    {
        private readonly Mock<IFileService> _mockFileService;

        public ItemServiceTests()
        {
            _mockFileService = new Mock<IFileService>();
            _mockFileService.Setup(fs => fs.CreateDirectory(It.IsAny<string>())).Returns(Task.CompletedTask);
            _mockFileService.Setup(fs => fs.DeleteDirectory(It.IsAny<string>())).Returns(Task.CompletedTask);
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

            var itemService = new ItemService(_mapper, context, _mockFileService.Object);


            // Act
            var result = await itemService.GetItems(5, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count());
        }

        [Fact]
        public async Task GetItemsCount_ShouldReturnCorrectCount()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            
            // Add test items to the database
            for (int i = 1; i <= 3; i++)
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

            var itemService = new ItemService(_mapper, context, _mockFileService.Object);

            // Act
            var result = await itemService.GetItemsCount();

            // Assert
            Assert.Equal(3, result);
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

            var itemService = new ItemService(_mapper, context, _mockFileService.Object);

            var expectedItem = new ReadExtendedItemDto
            {
                id_item = 1,
                reference_name_item = "REF-1",
                friendly_name_item = "Test Item",
                description_item = "Test Description",
                seuil_min_item = 10
            };

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
            
            var itemService = new ItemService(_mapper, context, _mockFileService.Object);

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

            var itemService = new ItemService(_mapper, context, _mockFileService.Object);

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

            var itemService = new ItemService(_mapper, context, _mockFileService.Object);

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

            var itemService = new ItemService(_mapper, context, _mockFileService.Object);

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