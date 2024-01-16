using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SalesBotApi.Models;
using Microsoft.Azure.Cosmos;
using System;
using Microsoft.Extensions.Configuration;

public class CosmosDbService
{
    public Container MessagesContainer { get; }
    public Container ConversationsContainer { get; }
    public Container CompaniesContainer { get; }
    public Container ChatbotsContainer { get; }
    public Container UsersContainer { get; }

    public CosmosDbService(IConfiguration configuration)
    {
        var client = new CosmosClient(
            "https://keli-chatbot-02.documents.azure.com:443/",
            "r5Mvqb5nf0G9uILKYTl0XTQHSMcxerm65qwm22ePQTIhQTxqnSPk8qosd2qaNjT0zx25XhK1i6jvACDbEcDLTg==",
            new CosmosClientOptions
            {
                ApplicationRegion = Regions.EastUS2,
            });

        var database = client.GetDatabase("keli");
        MessagesContainer = database.GetContainer("messages_sales");
        ConversationsContainer = database.GetContainer("conversations_sales");
        CompaniesContainer = database.GetContainer("companies_sales");
        ChatbotsContainer = database.GetContainer("chatbots_sales");
        UsersContainer = database.GetContainer("users_sales");
    }
}
