using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlloouBot
{
    public class FunThings
    {
        public int[] RollDice(int diceToRoll = 2, int numberOfSides = 6)
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            int[] values = new int[diceToRoll];

            for(int i = 0; i < diceToRoll; i++)
                values[i] = random.Next(1, numberOfSides);

            return values;
        }

        public string IsThisYourCard()
        {
            List<string> Suit = new List<string>() { "Hearts", "Clubs", "Spades", "Diamonds"};
            List<string> Rank = new List<string>() { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "King", "Queen", "Jack", "Ace"};
            Random random = new Random((int)DateTime.Now.Ticks);
            string sReturn = string.Empty;

            sReturn = string.Format("{0} of {1}", Rank.ElementAt(random.Next(Rank.Count -1)), Suit.ElementAt(random.Next(Suit.Count -1)));
            return sReturn;
        }

        /// <summary>
        /// Referencing Shadow The Hedgehog. Possible results are based on the game's endings
        /// </summary>
        /// <param name="userId">Used to determine who they are!</param>
        /// <returns></returns>
        public string ThisIsWhoIAm(string userId)
        {
            throw new NotImplementedException();
        }

        // I want this to generate random messages every time the bot loads. Might end up being time consuming to make.
        public string[] PopulateRandomMessages()
        {
            throw new NotImplementedException();
        }
    }
}
