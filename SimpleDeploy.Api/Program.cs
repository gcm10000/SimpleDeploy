using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Impl;
using SimpleDeploy.Application.Contexts;
using SimpleDeploy.Application.IoC;
using SimpleDeploy.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.ConfigureApplication();

builder.Services.AddQuartz(q =>
{
    //q.UseMicrosoftDependencyInjectionJobFactory();
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
builder.Services.AddSingleton<DeployUpdateJob>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();

ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
var scheduler = await schedulerFactory.GetScheduler();
await scheduler.Start();

var job = JobBuilder.Create<DeployUpdateJob>()
    .WithIdentity("deployUpdateJob", "group1")
    .Build();

var trigger = TriggerBuilder.Create()
    .WithIdentity("deployUpdateTrigger", "group1")
    .StartNow()
    .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever())
    .Build();

await scheduler.ScheduleJob(job, trigger);

app.Run();

