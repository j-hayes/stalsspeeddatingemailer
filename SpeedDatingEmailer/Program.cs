using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Com.CloudRail.SI.Services;
using CsvHelper;
using CsvHelper.Configuration;

namespace SpeedDatingEmailer
{
    class Program
    {

        static void Main(string[] args)
        {
            TextReader masterListReader = new StreamReader(@"input\Master Speed Dating List - 2019.csv");
            TextReader responsesReader = new StreamReader(@"input\Saint Alphonsus Speed Dating selections - 2019.csv");
            var masterListCsvReader = new CsvReader(masterListReader, new Configuration() { HeaderValidated = null, MissingFieldFound = null });
            var responseReader = new CsvReader(responsesReader, new Configuration() { HeaderValidated = null, MissingFieldFound = null });
            List<Participant> participants = masterListCsvReader.GetRecords<Participant>().ToList();
            
            List<Response> responses = responseReader.GetRecords<Response>().ToList();

            var male20s = new Dictionary<string, Participant>();
            var male30s = new Dictionary<string, Participant>();
            var female20s = new Dictionary<string, Participant>();
            var female30s = new Dictionary<string, Participant>();
            int emailsSent = 0;
            int matches = 0;

            foreach (var participant in participants)
            {
                participant.Id = Regex.Replace(participant.NameTag, "\\D", "");
                participant.IdNumber = int.Parse(participant.Id);
                if (participant.Event == EventConstants.Female20s)
                {
                    female20s.Add(participant.Id, participant);
                }
                else if (participant.Event == EventConstants.Male20s)
                {
                    male20s.Add(participant.Id, participant);
                }
                else if (participant.Event == EventConstants.Female30s)
                {
                    female30s.Add(participant.Id, participant);
                }
                else if (participant.Event == EventConstants.Male30s)
                {
                    male30s.Add(participant.Id, participant);
                }
            }

            var participantDictionary = participants.ToDictionary(x => x.Id, x => x);

            foreach (var response in responses)
            {
                response.IdNumber = Regex.Replace(response.NameNumber, "\\D", "");
                var selectionsStrings = response.Selections.Split(',');
                Participant participant;
                Dictionary<string, Participant> matchableParticipants = new Dictionary<string, Participant>();
                if (female20s.TryGetValue(response.IdNumber, out participant))
                {
                    matchableParticipants = male20s;
                }
                else if (female30s.TryGetValue(response.IdNumber, out participant))
                {
                    matchableParticipants = male30s;
                }
                else if (male20s.TryGetValue(response.IdNumber, out participant))
                {
                    matchableParticipants = female20s;
                }
                else if (male30s.TryGetValue(response.IdNumber, out participant))
                {
                    matchableParticipants = female30s;
                }
                else
                {
                    Console.WriteLine($"response with Id {response.NameNumber} did not match participant");
                }
                foreach (var selection in selectionsStrings)
                {
                    var matchId = Regex.Replace(selection, "\\D", "");
                    var matchFound = matchableParticipants.TryGetValue(matchId, out var match);
                    if (matchFound)
                    {
                        match.Matches.Add(response);
                    }
                    else
                    {
                        Console.WriteLine($"match between {participant.NameTag} {matchId} not found in allowed matches");
                    }
                }
            }
            var participantsWithExceptions = new List<Participant>();
            Participant lastParticpantWithoutError;
            string lastParticpantWithoutErrorId;


            foreach (var participant in participants.Where(x=>x.IdNumber > 101).OrderBy(x=> x.IdNumber))
            {
                Console.WriteLine($"Particpant {participant.FirstName} {participant.LastName} has {participant.Matches.Count}");

                try
                {
                    using (var message = new MailMessage())
                    {
                        using (var client = new SmtpClient("smtp.gmail.com"))
                        {
                            client.Port = 587;
                            client.Credentials = new NetworkCredential("stalspeeddate@gmail.com", " INSERT PASSWORRD HERE-dO NOT CHECK PASSWORD INTO SOURCE CONTROLL");
                            client.EnableSsl = true;

                            message.To.Add(new MailAddress(participant.Email, participant.Email));
                            message.From = new MailAddress("stalspeeddate@gmail.com", "stalspeeddate@gmail.com");
                            message.Subject = "Your speed dating results";
                            StringBuilder stringBuilder = new StringBuilder();
                            bool emailAddressNotValuid = false;
                            if (!participant.Matches.Any())
                            {
                                stringBuilder.AppendLine(EmailConstants.HAS_RESULTS_TEXT);
                                stringBuilder.AppendLine(EmailConstants.HAS_NO_RESULTS_TEXT);
                            }
                            else
                            {
                                stringBuilder.AppendLine(EmailConstants.HAS_RESULTS_TEXT);
                                stringBuilder.AppendLine(EmailConstants.INTEREST_TEXT_1);
                                stringBuilder.AppendLine(String.Empty);
                                foreach (var match in participant.Matches)
                                {
                                    if (string.IsNullOrWhiteSpace(participant.Email) ||
                                        string.IsNullOrWhiteSpace(match.Email))
                                    {
                                        emailAddressNotValuid = true;
                                    }

                                    string isAMatch = "";//no match
                                    if (participantDictionary.TryGetValue(match.IdNumber, out var matchingParticipant))
                                    {
                                        if(matchingParticipant.Matches.Any(x => x.IdNumber == participant.Id))
                                        {
                                            isAMatch = "- is a match";
                                            matches++;
                                        }
                                    }
                                    stringBuilder.AppendLine($"{match.FullName} - {match.IdNumber}: {match.Email} {isAMatch}");
                                }
                            }

                            stringBuilder.AppendLine(EmailConstants.END_TEXT);
                            stringBuilder.AppendLine(EmailConstants.NO_FORM_SUBMISSION4);
                            stringBuilder.AppendLine(EmailConstants.ThankYou);
                            stringBuilder.AppendLine(EmailConstants.StAlsYAMSocial);

                            message.Body = stringBuilder.ToString();
                            message.IsBodyHtml = false;

                        
                            if (!emailAddressNotValuid)
                            {
                                emailsSent++;
                                client.Send(message);
                            }
                            else
                            {
                            }
                        }
                    }

                    lastParticpantWithoutError = participant;
                    lastParticpantWithoutErrorId = participant.Id;
                }
                catch (Exception ex)
                {
                    participantsWithExceptions.Add(participant);
                }
            }

            Console.WriteLine($"Number of matches {matches} ");
            Console.WriteLine($"Number of emails sent {emailsSent} ");
            Console.WriteLine($"Number of emails responses {responses.Count}");
            Console.WriteLine($"Number of emails participants {participants.Count}");
            Console.WriteLine("Press enter to end");
            Console.ReadLine();
        }
    }
    public static class EventConstants
    {
        public const string Female20s = "Female 20's YAM Speed Dating";
        public const string Male20s = "Male 20's Yam Speed Dating";
        public const string Female30s = "Female 30's YAM Speed Dating";
        public const string Male30s = "Male 30's YAM Speed Dating";
    }

    public static class EmailConstants
    {
        public const string HAS_RESULTS_TEXT = "Thank you again for participating in Speed Dating at St. Alphonsus. This event is consistently one of our largest fundraisers for the St. Alphonsus Young Adult Community and helps fund most of our other programs throughout the year. We hope you had a great time at the event and had the opportunity to meet some of the wonderful people who participate in our community.";

        public const string MATCH_TEXT = "Without further ado, below is the list of people towards whom you expressed interest who also expressed interest in you:";
        public const string INTEREST_TEXT_1 = "Without further ado, here are people who are interested in getting to know you better (matches are marked as such):";
        public const string END_TEXT = "Don’t be shy! Be sure to reach out if you want to continue any conversations you started the other night.";
        public const string HAS_NO_RESULTS_TEXT = "Although we didn\";t find a connection for you at this time, we hope to continue seeing you at St. Alphonsus.";
        public const string NO_FORM_SUBMISSION1 = "Thank you again for participating in Speed Dating at St. Alphonsus. We note that as of now we have not received your form indicating the list of individuals whom you would be interested in getting to know further. The original deadline was today at 12pm. If you do not submit, we will be unable to send you the list of people who are interested in you.";
        public const string NO_FORM_SUBMISSION2 = "We kindly ask that you submit the form by today at 3pm. However, we will be officially closing the form at that time and sending out the results shortly thereafter.";
        public const string NO_FORM_SUBMISSION3 = "Even if you did not feel that you had a connection last night, we would appreciate if you would take a minute to fill out the form and provide some feedback on the event: http://bit.ly/StAlSpeedDating2018";
        public const string NO_FORM_SUBMISSION4 = "(If you were unable to attend the event, please disregard.)";
        public const string ThankYou = "Thank you!";
        public const string StAlsYAMSocial = "YAM Social Team";
    }

    public class Participant
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NameTag { get; set; }
        public string Event { get; set; }
        public string Email { get; set; }
        public string Id { get; set; }
        public int IdNumber { get; set; }
        public List<Response> Matches { get; set; } = new List<Response>();
    }

    public class Response
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string NameNumber { get; set; }
        public string Selections { get; set; }
        public string IdNumber { get; set; }
    }
}
