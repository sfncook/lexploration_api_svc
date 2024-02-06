using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SalesBotApi.Models;

public class HubspotService
{
    private readonly HttpClient _httpClient;
    private readonly LogBufferService logger;
    private readonly MetricsBufferService metrics;
    private readonly SharedQueriesService sharedQueriesService;
    
    public HubspotService(
        LogBufferService logger,
        MetricsBufferService metrics,
        SharedQueriesService sharedQueriesService
    ) {
        _httpClient = new HttpClient();
        this.logger = logger;
        this.metrics = metrics;
        this.sharedQueriesService = sharedQueriesService;
    }

    public async Task InitializeHubspotAccount(Company company) {
        logger.Info($"HubspotService.InitializeHubspotAccount company:{company.company_id}");
        try {
            await CreateHubspotGroup(company);
        } catch (Exception e) {
            logger.Error($"Failed to create Hubspot group for company {company.company_id}: {e.Message}");
            throw;
        }

        try {
            await CreateHubspotProperties(company);
        } catch (Exception e) {
            logger.Error($"Failed to create Hubspot property for company {company.company_id}: {e.Message}");
            throw;
        }

        try {
            await SetCompanyHubspotInitialized(company);
        } catch (Exception e) {
            logger.Error($"Failed to update company.hubspot_initialized {company.company_id}: {e.Message}");
            throw;
        }
    }

    public async Task UpdateContactObj(
        HubspotUpdateQueueMessage msg
    ) {
        Console.WriteLine("HubspotService.UpdateContactObj");

        var companyTask = sharedQueriesService.GetCompanyById(msg.company_id);
        var convoTask1 = sharedQueriesService.GetConversationById(msg.convo_id);
        await Task.WhenAll(companyTask, convoTask1);
        Company company = await companyTask;
        Conversation convo = await convoTask1;

        int hubspot_id = convo.hubspot_id;
        HubspotContact hubspotContact =  null;

        // First check if we need to get the hubspot contact and update our db with the hubspot contact ID as needed
        if(hubspot_id==0) {
            var emailTask = GetHubspotContact_By_Email(company, msg.user_email);
            var convoTask = GetHubspotContact_By_ConvoId(company, convo);
            Task.WaitAll(emailTask, convoTask);
            HubspotContact contactEmail = await emailTask;
            HubspotContact contactConvo = await convoTask;

            if(contactEmail != null) {
                hubspotContact = contactEmail;
                hubspot_id = hubspotContact.id;
            } else if(contactConvo != null) {
                hubspotContact = contactConvo;
                hubspot_id = hubspotContact.id;
            }

            // Update convo.hubspot_id if needed
            if(hubspot_id > 0){
                convo.hubspot_id = hubspot_id;
                await sharedQueriesService.PatchConversation(convo);
            }
        } else {
            hubspotContact = await GetHubspotContact_By_HubspotId(company, hubspot_id);
        }

        // If we don't have a valid hubspotContact then create a new contact else update the preexisting contact in hubspot
        if(hubspotContact==null) {
            hubspotContact = await CreateNewHubspotContact(company, convo, msg.user_email, msg.user_phone_number, msg.user_last_name, msg.user_first_name);
        } else {
            hubspotContact = await UpdateHubspotContact(company, convo, hubspotContact, msg.user_email, msg.user_phone_number, msg.user_last_name, msg.user_first_name);
        }

        // This is necessary if we created a new contact and it's just redundant double-checking otherwise.
        if(convo.hubspot_id==0 && hubspotContact!=null) {
            convo.hubspot_id = hubspotContact.id;
            await sharedQueriesService.PatchConversation(convo);
        }
    }

    private async Task<HubspotContact> CreateNewHubspotContact(
        Company company,
        Conversation convo,
        string user_email,
        string user_phone_number,
        string user_last_name,
        string user_first_name
    ) {
        HubspotUpdateRequestBody requestBody = new HubspotUpdateRequestBody
        {
            properties = new HubspotUpdateRequestBodyProperties {
                email = user_email,
                phone = user_phone_number,
                company = company.name,
                lastname = user_last_name,
                firstname = user_first_name,
                kelicompanyid = company.company_id,
                keliconvoid = convo.id
            }
        };
        string requestBodyStr = JsonConvert.SerializeObject(requestBody);
        var response = await SendHubspotRequest(company, HttpMethod.Post, "https://api.hubapi.com/crm/v3/objects/contacts", requestBodyStr);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<HubspotContact>(responseString);
    }
    private async Task<HubspotContact> UpdateHubspotContact(
        Company company,
        Conversation convo,
        HubspotContact hubspotContact,
        string user_email,
        string user_phone_number,
        string user_last_name,
        string user_first_name
    ) {
        HubspotUpdateRequestBody requestBody = new HubspotUpdateRequestBody
        {
            properties = new HubspotUpdateRequestBodyProperties {
                company = company.name,
                kelicompanyid = company.company_id,
                keliconvoid = convo.id
            }
        };
        if(!string.IsNullOrWhiteSpace(user_email)) {
            requestBody.properties.email = user_email;
        }
        if(!string.IsNullOrWhiteSpace(user_phone_number)) {
            requestBody.properties.phone = user_phone_number;
        }
        if(!string.IsNullOrWhiteSpace(user_last_name)) {
            requestBody.properties.lastname = user_last_name;
        }
        if(!string.IsNullOrWhiteSpace(user_first_name)) {
            requestBody.properties.firstname = user_first_name;
        }
        string requestBodyStr = JsonConvert.SerializeObject(requestBody);
        var response = await SendHubspotRequest(company, HttpMethod.Patch, $"https://api.hubapi.com/crm/v3/objects/contacts/{hubspotContact.id}", requestBodyStr);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<HubspotContact>(responseString);
    }

    private async Task<HubspotContact> GetHubspotContact_By_Email(
        Company company,
        string email
    ) {
        if(string.IsNullOrWhiteSpace(email)) {
            var response = await SendHubspotRequest(company, HttpMethod.Get, $"https://api.hubapi.com/crm/v3/objects/contacts/{email}?idProperty=email&properties=email&properties=phone&properties=company&properties=lastname&properties=firstname&properties=kelicompanyid&properties=keliconvoid", null);
            if(response.StatusCode == System.Net.HttpStatusCode.Forbidden) {
                logger.Error($"(1) Company {company.company_id} missing required scopes in Hubspot: {response.Content}");
                throw new Exception("(1) Your Hubspot 'private app' for Keli.AI is missing required scopes.");
            }
            if(response.IsSuccessStatusCode) {
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<HubspotContact>(responseString);
            }
        }
        return null;
    }

    private async Task<HubspotContact> GetHubspotContact_By_ConvoId(
        Company company,
        Conversation convo
    ) {
        var response = await SendHubspotRequest(company, HttpMethod.Get, $"https://api.hubapi.com/crm/v3/objects/deals/{convo.id}?idProperty=keliconvoid&properties=email&properties=phone&properties=company&properties=lastname&properties=firstname&properties=kelicompanyid&properties=keliconvoid", null);
        if(response.StatusCode == System.Net.HttpStatusCode.Forbidden) {
            logger.Error($"(2) Company {company.company_id} missing required scopes in Hubspot: {response.Content}");
            throw new Exception("(2) Your Hubspot 'private app' for Keli.AI is missing required scopes.");
        }
        if(response.IsSuccessStatusCode) {
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<HubspotContact>(responseString);
        }
        return null;
    }

    private async Task<HubspotContact> GetHubspotContact_By_HubspotId(
        Company company,
        int hubspot_id
    ) {
        var response = await SendHubspotRequest(company, HttpMethod.Get, $"https://api.hubapi.com/crm/v3/objects/contacts/{hubspot_id}?properties=email&properties=phone&properties=company&properties=lastname&properties=firstname&properties=kelicompanyid&properties=keliconvoid", null);
        if(response.StatusCode == System.Net.HttpStatusCode.Forbidden) {
            logger.Error($"(3) Company {company.company_id} missing required scopes in Hubspot: {response.Content}");
            throw new Exception("(3) Your Hubspot 'private app' for Keli.AI is missing required scopes.");
        }
        if(response.IsSuccessStatusCode) {
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<HubspotContact>(responseString);
        }
        return null;
    }
    private async Task CreateHubspotGroup(Company company) {
        var requestBody = new
        {
            name = "keliai",
            displayOrder = -1,
            label = "KeliAI Group"
        };
        string requestBodyStr = JsonConvert.SerializeObject(requestBody);
        var response = await SendHubspotRequest(company, HttpMethod.Post, "https://api.hubapi.com/crm/v3/properties/contact/groups", requestBodyStr);
        response.EnsureSuccessStatusCode();
    } 

    private async Task CreateHubspotProperties(Company company) {
        var requestBody = new
        {
            hidden =  false,
            displayOrder =  2,
            description =  "Unique identifier for this contact in the Keli.AI system",
            label =  "Keli.AI Contact ID",
            type =  "string",
            groupName =  "keliai",
            name =  "keliconvoid",
            fieldType =  "text",
            formField =  false,
            hasUniqueValue =  true
        };
        string requestBodyStr = JsonConvert.SerializeObject(requestBody);
        var convoTask = SendHubspotRequest(company, HttpMethod.Post, "https://api.hubapi.com/crm/v3/properties/contact", requestBodyStr);

        var requestBody2 = new
        {
            hidden =  false,
            displayOrder =  2,
            description =  "Unique identifier for your company in the Keli.AI system",
            label =  "Keli.AI Company ID",
            type =  "string",
            groupName =  "keliai",
            name =  "kelicompanyid",
            fieldType =  "text",
            formField =  false,
            hasUniqueValue =  false
        };
        string requestBodyStr2 = JsonConvert.SerializeObject(requestBody2);
        var companyTask = SendHubspotRequest(company, HttpMethod.Post, "https://api.hubapi.com/crm/v3/properties/contact", requestBodyStr2);

        await Task.WhenAll(convoTask, companyTask);
        var responseConvo = await convoTask;
        var responseCompany = await companyTask;
        responseConvo.EnsureSuccessStatusCode();
        responseCompany.EnsureSuccessStatusCode();
    } 

    private async Task SetCompanyHubspotInitialized(Company company) 
    {
        company.hubspot_initialized = true;
        await sharedQueriesService.PatchCompany(company);
    }

    private async Task<HttpResponseMessage> SendHubspotRequest(Company company, HttpMethod httpMethod, string url, string requestBodyStr) {
        string hubspot_access_token = company.hubspot_access_token;
        using (var requestMessage = new HttpRequestMessage(httpMethod, url))
        {
            if(requestBodyStr!=null) {
                var content = new StringContent(requestBodyStr, Encoding.UTF8, "application/json");
                requestMessage.Content = content;
            }
            requestMessage.Headers.Add("Authorization", $"Bearer {hubspot_access_token}");
            return await _httpClient.SendAsync(requestMessage);
        }
    }

    public class HubspotUpdateRequestBody {
        public HubspotUpdateRequestBodyProperties properties { get; set; }
    }
    public class HubspotUpdateRequestBodyProperties {
        public string email { get; set; }
        public string phone { get; set; }
        public string company { get; set; }
        public string lastname { get; set; }
        public string firstname { get; set; }
        public string kelicompanyid { get; set; }
        public string keliconvoid { get; set; }
    }
    public class HubspotContactProperties {
        public string company { get; set; }
        public string email { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string phone { get; set; }
    }
    public class HubspotContact {
        // {"id":"951","properties":{"company":null,"createdate":"2024-02-06T00:06:09.728Z","email":"a1@b.com","firstname":null,"hs_object_id":"951","lastmodifieddate":"2024-02-06T00:06:27.699Z","lastname":null,"phone":null},"createdAt":"2024-02-06T00:06:09.728Z","updatedAt":"2024-02-06T00:06:27.699Z","archived":false}
        public int id { get; set; }
        public HubspotContactProperties properties { get; set; }
    }

    public class HubspotUpdateQueueMessage {
        public string company_id { get; set; }
        public string convo_id { get; set; }
        public string user_first_name { get; set;}
        public string user_last_name { get; set;}
        public string user_email { get; set;}
        public string user_phone_number { get; set;}
        public string user_company_name { get; set;}
    }

}