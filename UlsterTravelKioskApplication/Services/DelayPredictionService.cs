using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlsterTravelKioskApplication.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UlsterTravelKioskApplication.Services
{
    // provides delay predictions
    public class DelayPredictionService
    {
        private readonly DataManager _data; // references data storage for accessing delay predictions


        public DelayPredictionService(DataManager data)
        {
            _data = data;
        }

        // returns todays delay prediction for selected airport
        public DelayPrediction DelayPredictionToday(string AirportCode)
        {
            // searches DelayPredictions list for a matching record
            var existing = _data.DelayPredictions.FirstOrDefault(p => p.AirportCode.Equals(AirportCode, StringComparison.OrdinalIgnoreCase) && p.Date.Date == DateTime.Today);

            // return if delay prediction exists for todays date
            if (existing != null)
            {
                return existing;
            }

            // creates default delay prediction if no record already created
            var delayPrediction = new DelayPrediction
            {
                AirportCode = AirportCode, // airport identifier
                Date = DateTime.Today, // assigns todays date
                Status = "On Time", // default status
                Percentage = 89 // default on time percentage
            };

            _data.DelayPredictions.Add(delayPrediction); // adds new delay prediction to the DElayPredictions list

            _data.SaveDelayPredictions(); // saves the new delay prediction to the Delay Predictions CSV file (delayPredictions.csv)

            return delayPrediction; // returns the delay prediction to the user
        }
    }
}
