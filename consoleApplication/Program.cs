using LiteDB;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace consoleApplication
{
    class Program
    {
        private static IManagedMqttClient client;
        private static bool sendMessage;
        private static List<MachineInfo> machineInfosList;
        private static List<SetupAssistent> setupAssListe;

        static void Main(string[] args)
        {

            



                
                sendMessage = true;

                 ConnectAsync();

                 client.UseConnectedHandler(e =>
                 {
                     Console.WriteLine("Connected successfully with MQTT Brokers.");
                 });
                 client.UseDisconnectedHandler(e =>
                 {
                     Console.WriteLine("Disconnected from MQTT Brokers.");
                 });

                 SubscribeAsync("sendMID");
                 SubscribeAsync("sendNewInstructionList");
                 SubscribeAsync("sendNewSetup");
                 SubscribeAsync("askForSAID");
                 while (true)
                 {




                     client.UseApplicationMessageReceivedHandler(e =>
                     {
                         try
                         {
                             string topic = e.ApplicationMessage.Topic;

                             if (string.IsNullOrWhiteSpace(topic) == false)
                             {
                                
                                 sendMessage = false;
                                 string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                                 Console.WriteLine($"Topic: {topic}. Message Received: {payload}");
                                 if(sendMessage == false)
                                 {
                                     using (var db = new LiteDatabase(@"C:\Datenbank\machineInfoDataBank.db"))
                                     {
                                         var machineInfoCollection = db.GetCollection<MachineInfo>("machineInfo");

                                         var instructionCollection = db.GetCollection<Instruction>("instruction");

                                         var setupAssCollection = db.GetCollection<SetupAssistent>("setupAssistent");

                                         //var maxIDInstructionList = instructionCollection.FindOne(Query.All(Query.Descending));
                                         
                                         
                                      
                                            

                                         if (topic.Equals("sendNewInstructionList"))
                                         {
                                             
                                             var newInstructionList = payload.ToString();
                                             var instructions = JsonSerializer.Deserialize(newInstructionList);
                                             var doc = BsonMapper.Global.ToDocument(instructions);
                                             var testObject = BsonMapper.Global.ToObject<InstructionList>(doc);
                                             Console.WriteLine(testObject.instructionList.ElementAt(0).description);

                                          

                                            

                                             foreach(var item in testObject.instructionList)
                                             {
                                                 instructionCollection.Insert(item);
                                             }





                                         }
                                         else if (topic.Equals("sendNewSetup"))
                                         {
                                             var newSetupList = payload.ToString();
                                             var setups = JsonSerializer.Deserialize(newSetupList);
                                             var doc = BsonMapper.Global.ToDocument(setups);
                                             var testObject = BsonMapper.Global.ToObject<SetupAssistent>(doc);






                                             setupAssCollection.Insert(testObject);


                                         }
                                         else if (topic.Equals("askForSAID"))
                                         {
                                             var results = setupAssCollection.Query()
                                            .OrderByDescending(x => x.sAID)
                                            .First();
                                             Console.WriteLine(results.sAID);
                                             PublishAsync("sendSAID", results.sAID);
                                         }
                                         else
                                         {
                                            
                                             
                                             var lookingForId = payload.ToString();
                                             var mInfo = machineInfoCollection.FindAll().Where(x => x.MachineId == lookingForId).FirstOrDefault();

                                             string machineId = mInfo.MachineId;

                                             
                                             Console.WriteLine(machineId);

                                             
                                             List<Instruction> instructionList = new List<Instruction>();

                                             List<SetupAssistent> setupAssListe = new List<SetupAssistent>();

                                             setupAssListe = (List<SetupAssistent>)setupAssCollection.FindAll().Where(x => x.machineID.Equals(machineId)).ToList();



                                             instructionList = (List<Instruction>)instructionCollection.FindAll().Where(x => x.machineID.Equals(machineId)).ToList();



                                             mInfo.instructionList = instructionList;
                                             mInfo.setupAssistentList = setupAssListe;


                                             Console.WriteLine(mInfo.instructionList.Count());

                                             var doc = BsonMapper.Global.ToDocument(mInfo);

                                             var jsonString = JsonSerializer.Serialize(doc);

                                             Console.WriteLine(jsonString);




                                             PublishAsync("sendMInfo", jsonString);
                                             

                                         }


                                         sendMessage = true; 

                                     }

                                 }


                             }
                         }
                         catch (Exception ex)
                         {
                             Console.WriteLine(ex.Message, ex);
                         }
                     });

                 }





            }


        public static async void ConnectAsync()
        {
            string clientId = "mqttx_47e23623";
            string mqttHost = "ws://broker.emqx.io:8083/mqtt";
            //string mqttUser = "";
            //string mqttPassword = "";
            //int mqttPort = 8083;
            bool mqttSecure = false;

            var messageBuilder = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithWebSocketServer(mqttHost)
                .WithCleanSession();

            var options = mqttSecure ? messageBuilder.WithTls().Build() : messageBuilder.Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options)
                .Build();

             client = new MqttFactory().CreateManagedMqttClient();


            

            await client.StartAsync(managedOptions);

        }

        public static async void PublishAsync(string topic, string payload, bool retainFlag = true, int qos = 1) =>
            await client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                .WithRetainFlag(retainFlag)
                .Build());

        public static async void SubscribeAsync(string topic, int qos = 1) =>
            await client.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                .Build());

        


    }

}
