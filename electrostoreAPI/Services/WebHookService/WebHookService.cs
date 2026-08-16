using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandService;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace ElectrostoreAPI.Services.WebHookService;

public class WebHookService : IWebHookService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ICommandService _commandService;
    private readonly ILogger<WebHookService> _logger;
    public WebHookService(IMapper mapper, ApplicationDbContext context, IConfiguration configuration, ICommandService commandService, ILogger<WebHookService> logger)
    {
        _mapper = mapper;
        _context = context;
        _configuration = configuration;
        _commandService = commandService;
        _logger = logger;
    }

    public async Task Process17TrackWebhook(JsonElement body, string signatureHeader)
    {
        if (_configuration.GetValue<bool>("Track17:Enable") == false)
        {
            _logger.LogWarning("17Track webhook processing is disabled");
            throw new ArgumentException("17Track webhook processing is disabled.");
        }
        var apiKey = _configuration.GetValue<string>("Track17:ApiKey");
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("17Track API key is not configured");
            throw new ArgumentException("API key is not configured.");
        }

        var signature = getGeneratedSignature(JsonSerializer.Serialize(body), apiKey);
        if (signatureHeader != signature)
        {
            _logger.LogWarning("Invalid 17Track webhook signature. Received: {Received}, Expected: {Expected}", signatureHeader, signature);
            throw new ArgumentException("Invalid signature.");
        }
        var event17Track = body.GetProperty("event").GetString();
        var data17Track = body.GetProperty("data");
        if (event17Track != "TRACKING_UPDATED")
        {
            _logger.LogWarning("Ignoring 17Track event: {Event}", event17Track);
            throw new ArgumentException($"Ignoring event: {event17Track}");
        }
        if (!data17Track.TryGetProperty("number", out var trackingNumberProperty) || !data17Track.TryGetProperty("carrier", out var carrierIdProperty))
        {
            _logger.LogWarning("Invalid 17Track webhook data format: missing 'number' or 'carrier' property");
            throw new ArgumentException("Invalid data format: missing 'number' or 'carrier' property.");
        }
        var trackingNumber = data17Track.GetProperty("number").GetString();
        var carrierId = data17Track.GetProperty("carrier").GetInt32();
        var commands = _context.Commands.Where(c => c.tracking_number == trackingNumber && c.id_carrier == carrierId).ToList();
        // fetch latest tracking status and sub-status from the data
        var latestStatus = data17Track.GetProperty("track_info").GetProperty("latest_status").GetProperty("status").GetString();
        var latestSubStatus = data17Track.GetProperty("track_info").GetProperty("latest_status").GetProperty("sub_status").GetString();
        if (!Enum.TryParse<TrackingStatus>(latestStatus, true, out var parsedStatus) || !Enum.TryParse<TrackingSubStatus>(latestSubStatus, true, out var parsedSubStatus))
        {
            _logger.LogWarning("Invalid status or sub-status received from 17Track: {Status}, {SubStatus}", latestStatus, latestSubStatus);
            throw new ArgumentException("Invalid status or sub-status received.");
        }
        foreach (var command in commands)
        {
            if (command.last_status != parsedStatus || command.last_sub_status != parsedSubStatus)
            {
                command.last_status = parsedStatus;
                command.last_sub_status = parsedSubStatus;
                command.raw_data = data17Track.GetRawText();
                command.shipper_address = data17Track.GetProperty("track_info").GetProperty("shipping_info").GetProperty("shipper_address").GetRawText();
                command.recipient_address = data17Track.GetProperty("track_info").GetProperty("shipping_info").GetProperty("recipient_address").GetRawText();
                _context.Commands.Update(command);

                var historyEntry = new CommandsHistory
                {
                    id_command = command.id_command,
                    status = parsedStatus,
                    sub_status = parsedSubStatus,
                    description = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("description").GetString(),
                    location = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("location").GetString(),
                    stage = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("stage").GetString(),
                    event_time_utc = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("time_utc").GetDateTime(),
                    timezone = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("time_raw").GetProperty("timezone").GetString(),
                    country = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("address").GetProperty("country").GetString(),
                    state = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("address").GetProperty("state").GetString(),
                    city = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("address").GetProperty("city").GetString(),
                    postal_code = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("address").GetProperty("postal_code").GetString(),
                    latitude = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("address").GetProperty("coordinates").TryGetProperty("latitude", out var latProp) ? latProp.GetString() : null,
                    longitude = data17Track.GetProperty("track_info").GetProperty("latest_event").GetProperty("address").TryGetProperty("coordinates", out var longProp) ? longProp.GetString() : null
                };
                _context.CommandsHistory.Add(historyEntry);
                await _context.SaveChangesAsync();
            }
        }
        _logger.LogInformation("17Track webhook processed successfully for tracking number {TrackingNumber}", trackingNumber);
    }

    private static string getGeneratedSignature(string requestText, string key)
    {
        var src = requestText + "/" + key;
        byte[] hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(src));

        var hexString = new System.Text.StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            hexString.Append(b.ToString("x2"));
        }
        return hexString.ToString();
    }
}