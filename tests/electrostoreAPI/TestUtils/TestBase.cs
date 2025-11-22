using Microsoft.EntityFrameworkCore;
using System;
using Moq;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using electrostore;
using electrostore.Dto;

namespace electrostore.Tests.Utils
{
    public abstract class TestBase
    {
        protected readonly IMapper _mapper;
        protected readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

        protected TestBase()
        {
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            
            var loggerFactory = LoggerFactory.Create(builder => { });
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, loggerFactory);
            _mapper = mapperConfig.CreateMapper();
        }
    }
}
