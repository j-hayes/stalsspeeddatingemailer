using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StAlsSpeedDatingScheduler.Console
{
    class ScheduleCreator
    {
        static void Main(string[] args)
        {
            var random = new Random();
            var men = new List<Participant>();
            var women = new List<Participant>();
            for (int i = 1; i < 101; i++)
            {
                var participant = new Participant();
                var genderRandom = random.Next(0,100);
                var age = random.Next(21,39);
                participant.Age = age;
                var minDaterAge = (age / 2) + 7;
                participant.MinDaterAge = minDaterAge >= 21 ? minDaterAge : 21;
                participant.MaxDaterAge = (age - 7) * 2;

                if (i > 52)
                {
                    participant.Gender = Gender.Female;
                    women.Add(participant);
                }
                else
                {
                    participant.Gender = Gender.Male;
                    men.Add(participant);
                }

                participant.ChairNumber = i;
            }

            foreach (var woman in women)
            {
                var pairings = men.Where(x => woman.WithinDatingRange(x))
                    .Select(x => new Pairing()
                    {
                        Woman = woman, Man = x
                    }).OrderBy(x=>x.AgeGap).ToList();
                woman.PairableParticipantsStack = new Stack<Pairing>(pairings);
                woman.PairableParticipants = pairings;

                foreach (var pairing in pairings)
                {
                    pairing.Man.PairableParticipants.Add(pairing);
                }
            }



            int timeSlots = 100;//(int)Math.Round((2.5d * 60d) / 5d) ;// 3 hours * 60 minutes / 5 minutes per round

            for (int i = 0; i < timeSlots; i++)
            {
                var menWithDatesForThisRound = new HashSet<Guid>();
                women.Shuffle();//makes sure the bias in the algorithm that favors being at the front of the list is randomized
                foreach (var woman in women)
                {
                    var dateNotFound = true;
                    var pairableParticipantsWithDateStack = new Stack<Pairing>();
                    while (dateNotFound && woman.PairableParticipantsStack.TryPop(out var potentialDate))
                    {
                        if (menWithDatesForThisRound.Contains(potentialDate.Man.Id))
                        {
                            pairableParticipantsWithDateStack.Push(potentialDate);
                            if (woman.NumberOfDatesInARow >= 6)
                            {
                                break;
                            }
                            
                        }
                        else
                        {
                            woman.Dates[i] = potentialDate.Man;
                            if (!potentialDate.Man.Dates.ContainsKey(i))
                            {
                                potentialDate.Man.Dates[i] = woman;
                            }
                            else
                            {
                                throw new Exception($"collision in round {i} from man {potentialDate.Man.Id} with woman {woman.Id}");
                            }
                            menWithDatesForThisRound.Add(potentialDate.Man.Id);
                            dateNotFound = false;
                            woman.NumberOfDatesInARow++;
                        }
                    }

                    if (dateNotFound)
                    {
                        woman.NumberOfDatesInARow = 0;
                    }
                    while (pairableParticipantsWithDateStack.TryPop(out var pairable))//put the closer in age participants back
                    {
                        woman.PairableParticipantsStack.Push(pairable);
                    }
                }
            }
            GenerateCSV(men.ToDictionary(x=>x.Id,x=>x),women.ToDictionary(x => x.Id, x => x));
            System.Console.WriteLine("Press any key to exit");

            System.Console.ReadLine();
        }

        private static void GenerateCSV(Dictionary<Guid, Participant> men, Dictionary<Guid, Participant> women)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Women");
            foreach (var woman in women)
            {
                string rounds = $"{woman.Value.ChairNumber},";
                for (int i = 0; i < 101; i++)
                {
                    if (woman.Value.Dates.TryGetValue(i, out var date))
                    {
                        rounds += $"{date.ChairNumber},"; // todo name

                    }
                    else
                    {
                        rounds += $"B,"; // todo name
                    }

                }
                stringBuilder.AppendLine(rounds);
            }
            stringBuilder.AppendLine("Men");

            foreach (var man in men)
            {
                string rounds = $"{man.Value.ChairNumber},";
                for (int i = 0; i < 101; i++)
                {
                    if (man.Value.Dates.TryGetValue(i, out var date))
                    {
                        rounds += $"{date.ChairNumber},"; // todo name
                    }
                    else
                    {
                        rounds += $"B,"; // todo name
                    }

                }
                stringBuilder.AppendLine(rounds);
            }
            System.Console.Write(stringBuilder.ToString());
        }
    }

    public static class ListExtensions{
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class Pairing
    {
        public Participant Woman { get; set; }
        public Participant Man { get; set; }

        public decimal AgeGap => Math.Abs(Man.Age - Woman.Age);

        public static bool CanPair(Participant man, Participant woman)
        {
            return man.WithinDatingRange(woman) && woman.WithinDatingRange(man);
        }

        public override string ToString()
        {
            return $"Age Gap = {this.AgeGap.ToString()})";
        }

    }

    public class Participant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int NumberOfDatesInARow { get; set; }
        public int ChairNumber { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public decimal MaxDaterAge { get; set; }
        public decimal MinDaterAge { get; set; }
        public Gender Gender { get; set; }
        public Stack<Pairing> PairableParticipantsStack { get; set; } = new Stack<Pairing>();
        public List<Pairing> PairableParticipants { get; set; } = new List<Pairing>();

        public Dictionary<int, Participant> Dates { get; set; } = new Dictionary<int, Participant>();

        public override string ToString()
        {
            return $"# of dates: {this.Dates.Count} # of potential dates {this.PairableParticipants.Count} age:{this.Age.ToString()}";
        }
        public bool WithinDatingRange(Participant otherParticipant)
        {
            return otherParticipant.Age <= this.MaxDaterAge && otherParticipant.Age >= this.MinDaterAge;
        }
    }

    public enum Gender
    {
        Female = 1,
        Male = 2
    }
}
