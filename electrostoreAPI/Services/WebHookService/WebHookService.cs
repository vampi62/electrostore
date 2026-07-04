using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandService;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ElectrostoreAPI.Services.WebHookService;

public class WebHookService : IWebHookService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ICommandService _commandService;
    public WebHookService(IMapper mapper, ApplicationDbContext context, IConfiguration configuration, ICommandService commandService)
    {
        _mapper = mapper;
        _context = context;
        _configuration = configuration;
        _commandService = commandService;
    }

    public Task<bool> Process17TrackWebhook(string body)
    {
        if (_configuration.GetValue<bool>("17Track:Enable") == false)
        {
            Console.WriteLine("17Track webhook processing is disabled.");
            return Task.FromResult(false);
        }
        var apiKey = _configuration.GetValue<string>("17Track:ApiKey");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("API key is not configured.");
            return Task.FromResult(false);
        }
        var signature = getGeneratedSignature(body, apiKey);
        Console.WriteLine($"Generated Signature: {signature}");
        /* if (!Request.Headers.TryGetValue("X-17Track-Signature", out var receivedSignature) || receivedSignature != signature)
        {
            Console.WriteLine("Invalid signature.");
            return Task.FromResult(false);
        } */

        // Implement the logic to process the webhook data here


        return Task.FromResult(true);
    }

    private string getGeneratedSignature(string requestText, string key)
    {
        var src = requestText + "/" + key;

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(src));

        var hexString = new System.Text.StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            hexString.Append(b.ToString("x2"));
        }
        return hexString.ToString();
    }
}