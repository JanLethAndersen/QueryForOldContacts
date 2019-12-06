using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Metadata;
using System.Activities;

namespace QueryForOldContacts
{
    public class findOldContacts : CodeActivity
    {
        [Input("username")]
        public InArgument<string> username { get; set; }

        [Input("birthdate")]
        public InArgument<DateTime> birthdate { get; set; }

        [Input("firstname")]
        public InArgument<string> firstname { get; set; }

        [Input("lastname")]
        public InArgument<string> lastname { get; set; }

        [Output("contact ID")]
        public OutArgument<string> contactID { get; set; }

        [Output("fullname")]
        public OutArgument<string> fullnameOutput { get; set; }

        [Output("omkstedID")]
        public OutArgument<string> omkStedId { get; set; }

        [Output("domæne")]
        public OutArgument<string> domaene { get; set; }

        [Output("email domæne")]
        public OutArgument<string> emailDomaene { get; set; }

        [Output("lokation")]
        public OutArgument<string> lokation { get; set; }

        [Output("stillingsbetegnelse")]
        public OutArgument<string> stillingsBetegnelse { get; set; }

        [Output("birthdate output")]
        public OutArgument<DateTime> birthDateOutput { get; set; }

        [Output("username output")]
        public OutArgument<string> usernameOutput { get; set; }



        public QueryExpression query { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            try
            {
                // Get the context service.
                IWorkflowContext Icontext = context.GetExtension<IWorkflowContext>();
                IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();

                // Use the context service to create an instance of IOrganizationService.             
                IOrganizationService service = serviceFactory.CreateOrganizationService(Icontext.InitiatingUserId);

                //get input fields
                var BirthDate = birthdate.Get(context) == new DateTime(0001, 01, 01) ? new DateTime(1753, 01, 01) : birthdate.Get(context);
                var FirstNameInput = firstname.Get(context);
                var LastNameInput = lastname.Get(context);
                var Username = username.Get(context) == null ? "" : username.Get(context);

                /* // lav query
                 var query = new QueryExpression("contact");
                 query.ColumnSet.AddColumns("sdu_brugernavn", "fullname", "birthdate", "contactid", "firstname", "lastname");

                 // alle skal have de samme fødselsdato
                 query.Criteria = new FilterExpression();
                 query.Criteria.AddCondition("birthdate", ConditionOperator.Equal, BirthDate);

                 // enten brugernavn eller fuldt navn match
                 FilterExpression userNameAndNameOr = new FilterExpression(LogicalOperator.Or);
                 userNameAndNameOr.AddCondition("sdu_brugernavn", ConditionOperator.Equal, Username);

                 FilterExpression UserNameAndNameAnd = new FilterExpression(LogicalOperator.Or);

                 UserNameAndNameAnd.AddCondition("fullname", ConditionOperator.Like, "%" + FullnameInput + "%");*/

                // made in fetchXML builder 
                // Instantiate QueryExpression QEcontact
                var QEcontact = new QueryExpression("contact");

                // Add columns to QEcontact.ColumnSet
                QEcontact.ColumnSet.AddColumns("fullname", "firstname", "lastname", "birthdate", "sdu_brugernavn", "sdu_domne", "emailaddress1", "address1_city", "jobtitle", "sdu_crmomkostningssted", "sdu_brugernavn", "address1_line1");

                // Define filter QEcontact.Criteria // all must match birthdate + sdu_crmudlb + opdateret fra fim
                var QEcontact_Criteria = new FilterExpression();
                QEcontact.Criteria.AddFilter(QEcontact_Criteria);

                // Define filter QEcontact_Criteria // either match on username, or firstname + lastname
                QEcontact_Criteria.FilterOperator = LogicalOperator.Or;
                QEcontact_Criteria.AddCondition("sdu_brugernavn", ConditionOperator.Equal, Username);

                var QEcontact_Criteria_name = new FilterExpression();
                QEcontact_Criteria.AddFilter(QEcontact_Criteria_name);

                // Define filter QEcontact_Criteria_name_birthdate
                QEcontact_Criteria_name.AddCondition("firstname", ConditionOperator.Like, "%" + FirstNameInput + "%");
                QEcontact_Criteria_name.AddCondition("lastname", ConditionOperator.Like, "%" + LastNameInput + "%");
                QEcontact_Criteria_name.AddCondition("birthdate", ConditionOperator.On, BirthDate);

                var QEcontact_Criteria_state = new FilterExpression();
                QEcontact.Criteria.AddFilter(QEcontact_Criteria_state);

                // Define filter QEcontact_Criteria_state
                QEcontact_Criteria_state.FilterOperator = LogicalOperator.Or;
                QEcontact_Criteria_state.AddCondition("statecode", ConditionOperator.Equal, 1); // kontoen er inaktiv

                var QEcontact_Criteria_dates = new FilterExpression();
                QEcontact_Criteria_state.AddFilter(QEcontact_Criteria_dates);

                // define filter QEcontact_Criteria_dates
                QEcontact_Criteria_dates.AddCondition("sdu_crmudlb", ConditionOperator.OnOrAfter, DateTime.Today.AddMonths(-13)); // CRM udløb er maksimalt 13 måneder gammelt
                QEcontact_Criteria_dates.AddCondition("sdu_crmudlb", ConditionOperator.OnOrBefore, DateTime.Today.AddDays(-1)); // CRM er igår eller før


                //find records
                var queryResult = service.RetrieveMultiple(QEcontact);

                if (queryResult.Entities.Count == 1)
                {

                    foreach (var record in queryResult.Entities)
                    {
                        // fullname
                        fullnameOutput.Set(context, record.GetAttributeValue<string>("fullname"));

                        // contactid
                        contactID.Set(context, record.GetAttributeValue<Guid>("contactid").ToString());

                        // omkostningssted
                        var omkStedIdLocal = searchForRecord(service, "sdu_brugeradmomksted",
                             new KeyValuePair<string, string>("sdu_omksted", record.GetAttributeValue<EntityReference>("sdu_crmomkostningssted").Id.ToString()),
                             new KeyValuePair<string, string>("", ""),
                             "sdu_brugeradmomkstedid");

                        omkStedId.Set(context, omkStedIdLocal);

                        // domæne 
                        String[] seperator_domaene = { "_" };

                        if (omkStedIdLocal != "")
                        {
                            domaene.Set(context, searchForRecord(service,
                                  "sdu_domner",
                                  new KeyValuePair<string, string>("sdu_brugeradmomksted", omkStedIdLocal.Split(seperator_domaene, StringSplitOptions.RemoveEmptyEntries).GetValue(0).ToString()),
                                  new KeyValuePair<string, string>("sdu_name", record.GetAttributeValue<string>("sdu_domne")),
                                  "sdu_domnerid"));

                            // email domæne
                            String[] seperator_emailDomaene = { "@" };
                            var emailDomainFromContact = "@" + record.GetAttributeValue<string>("emailaddress1").Split(seperator_emailDomaene, StringSplitOptions.RemoveEmptyEntries).GetValue(1).ToString();

                            emailDomaene.Set(context, searchForRecord(service,
                                "sdu_emaildomne",
                                new KeyValuePair<string, string>("sdu_brugeradmomksted", omkStedIdLocal.Split(seperator_domaene, StringSplitOptions.RemoveEmptyEntries).GetValue(0).ToString()),
                                new KeyValuePair<string, string>("sdu_name", emailDomainFromContact.Replace(" ", "")), // remove whitespaces
                                "sdu_emaildomneid"));
                        }
                        else
                        {
                            // set output parameters to empty strings, if no omk sted
                            domaene.Set(context, "");
                            emailDomaene.Set(context, "");

                        }
                                                
                        // lokation + arbejdsadresse
                        var LokationOptionSetValue = "";

                        switch (record.GetAttributeValue<string>("address1_city"))
                        {
                            case "Odense":
                                LokationOptionSetValue = "100000000";
                                break;
                            case "Odense M":
                                LokationOptionSetValue = "100000000";
                                break;
                            case "Sønderborg":
                                LokationOptionSetValue = "100000001";
                                break;
                            case "Esbjerg":
                                LokationOptionSetValue = "100000002";
                                break;
                            case "Slagelse":
                                LokationOptionSetValue = "100000003";
                                break;
                            case "Kolding":
                                LokationOptionSetValue = "100000004";
                                break;
                            default:
                                break;
                        }

                        var workAddress = record.GetAttributeValue<string>("address1_line1");

                        if(workAddress.Contains("Campusvej"))
                        {
                            LokationOptionSetValue = LokationOptionSetValue + "_" + "100000000";
                        } else if(workAddress.Contains("J.B. Winsløws Vej")) {
                            LokationOptionSetValue = LokationOptionSetValue + "_" + "100000001";
                        }

                        lokation.Set(context, LokationOptionSetValue);


                        // stillingsbetegnelse
                        stillingsBetegnelse.Set(context, searchForRecord(service,
                            "sdu_stikogruppe",
                            new KeyValuePair<string, string>("sdu_name", record.GetAttributeValue<string>("jobtitle")),
                            new KeyValuePair<string, string>("", ""),
                            "sdu_stikogruppeid"));

                        // fødselsdato
                        birthDateOutput.Set(context, record.GetAttributeValue<DateTime>("birthdate"));

                        // brugernavn
                        usernameOutput.Set(context, record.GetAttributeValue<string>("sdu_brugernavn"));

                    }
                }

            }
            catch (Exception ex)
            {
                throw (ex);
            }


        }

     public string searchForRecord(IOrganizationService service, string entity, KeyValuePair<string,string> firstKVP, KeyValuePair<string, string> secondKVP, string fieldToExtract)
        {
            try
            {
                var brugerAdmOmkId = "";


                var query = new QueryExpression(entity);
                query.ColumnSet = new ColumnSet(true);

                ConditionExpression firstCondition = new ConditionExpression
                {
                    AttributeName = firstKVP.Key,
                    Operator = ConditionOperator.Equal,
                    Values = { firstKVP.Value }
                };

                ConditionExpression secondCondition = new ConditionExpression
                {
                    AttributeName = secondKVP.Key,
                    Operator = ConditionOperator.Equal,
                    Values = { secondKVP.Value }
                };

                FilterExpression filter = new FilterExpression();
                filter.AddCondition(firstCondition);

                if (secondKVP.Key != "")
                {
                    filter.AddCondition(secondCondition);
                }

                query.Criteria.AddFilter(filter);

                DataCollection<Entity> queryResult = service.RetrieveMultiple(query).Entities;

                foreach (var result in queryResult)
                {
                    brugerAdmOmkId = result.GetAttributeValue<Guid>(fieldToExtract).ToString() + "_" + result.GetAttributeValue<string>("sdu_name").ToString();
                }

                return brugerAdmOmkId;

            }
            catch (Exception ex)
            {

                throw(ex);
            }
 
        }
    }
}
