using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using Domain.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class TaskProccesingWorker : BackgroundService
{
    private readonly ChannelReader<string> channelReader;
    private readonly ILogger<TaskProccesingWorker> logger;

    public TaskProccesingWorker(ChannelReader<string> _channelReader, ILogger<TaskProccesingWorker> _logger)
    {
        channelReader = _channelReader;
        logger = _logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Воркер обработки TaskItem запущен");

        try
        {
            await foreach (var message in channelReader.ReadAllAsync(stoppingToken))
            {
                logger.LogInformation(message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Произошла критическая ошибка в работе воркера");
        }

        logger.LogInformation("Воркер завершил работу");
    }
}