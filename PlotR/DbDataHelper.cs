using DotSpatial.Positioning;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlotR
{
    class DbDataHelper
    {
        /// <summary>
        /// Bad velocity value.
        /// </summary>
        public const double BAD_VELOCITY = 88.888;

        /// <summary>
        /// Bad amplitude.
        /// </summary>
        public const double BAD_AMPLITUDE = 0.0;

        /// <summary>
        /// Bad Bottom Value.
        /// </summary>
        public const int BAD_BOTTOM_BIN = 0;

        #region Class

        /// <summary>
        /// Object to hold the magnitude and direction and the
        /// average values.
        /// </summary>
        public class VelocityMagDir
        {
            /// <summary>
            /// Magnitude of the water for each bin.
            /// </summary>
            public double[] Magnitude { get; set; }

            /// <summary>
            /// Direction with X North for each bin.
            /// </summary>
            public double[] DirectionXNorth { get; set; }

            /// <summary>
            /// Directions with Y North for each bin.
            /// </summary>
            public double[] DirectionYNorth { get; set; }

            /// <summary>
            /// Average Magnitude.
            /// </summary>
            public double AvgMagnitude { get; set; }

            /// <summary>
            /// Average Direction X North.
            /// </summary>
            public double AvgDirectionXNorth { get; set; }

            /// <summary>
            /// Average Direction with Y North.
            /// </summary>
            public double AvgDirectionYNorth { get; set; }

            /// <summary>
            /// Bin number for the range.
            /// </summary>
            public int RangeBin { get; set; }

            /// <summary>
            /// Bottom Track Velocities are good.
            /// </summary>
            public bool IsBtVelGood { get; set; }

            /// <summary>
            /// Bottom Track East Velocity.
            /// Only store good values.
            /// Used to have a backup value.
            /// </summary>
            public double BtEastVel { get; set; }

            /// <summary>
            /// Bottom Track North Velocity.
            /// Only store good values.
            /// Used to have a backup value.
            /// </summary>
            public double BtNorthVel { get; set; }

            /// <summary>
            /// Initialize the values.
            /// </summary>
            public VelocityMagDir()
            {
                Magnitude = null;
                DirectionXNorth = null;
                DirectionYNorth = null;
                AvgMagnitude = 0.0;
                AvgDirectionXNorth = 0.0;
                AvgDirectionYNorth = 0.0;
                RangeBin = 0;
                IsBtVelGood = false;
                BtEastVel = BAD_VELOCITY;
                BtNorthVel = BAD_VELOCITY;
            }

            /// <summary>
            /// Initialize with the number of bins.
            /// </summary>
            /// <param name="numBins">Number of bins.</param>
            public VelocityMagDir(int numBins)
            {
                Magnitude = new double[numBins];
                DirectionXNorth = new double[numBins];
                DirectionYNorth = new double[numBins];
                AvgMagnitude = 0.0;
                AvgDirectionXNorth = 0.0;
                AvgDirectionYNorth = 0.0;
                RangeBin = 0;
                IsBtVelGood = false;
                BtEastVel = BAD_VELOCITY;
                BtNorthVel = BAD_VELOCITY;
            }
        }

        /// <summary>
        /// GPS data.
        /// </summary>
        public class GpsData
        {
            /// <summary>
            /// VTG message for GPS speed.
            /// </summary>
            public GpvtgSentence GPVTG { get; set; }

            /// <summary>
            /// GGA message for the latitude and longitude.
            /// </summary>
            public GpggaSentence GPGGA { get; set; }

            /// <summary>
            /// HDT message for the heading.
            /// </summary>
            public GphdtSentence GPHDT { get; set; }

            /// <summary>
            /// RMC message.
            /// </summary>
            public GprmcSentence GPRMC { get; set; }

            /// <summary>
            /// GPS Position (Lat and Lon)
            /// </summary>
            public Position Position { get; set; }

            /// <summary>
            /// GPS Bearing value.
            /// </summary>
            public Azimuth Bearing { get; set; }

            /// <summary>
            /// GPS Heading value.
            /// </summary>
            public Azimuth Heading { get; set; }

            /// <summary>
            /// GPS Speed value.
            /// </summary>
            public Speed Speed { get; set; }

            /// <summary>
            /// This value can be used as a backup ship speed for east component.
            /// </summary>
            public double BackupShipEast { get; set; }

            /// <summary>
            /// This value can be used as a backup ship speed for North component.
            /// </summary>
            public double BackupShipNorth { get; set; }

            /// <summary>
            /// Set flag if the Backup speed is good or bad.
            /// </summary>
            public bool IsBackShipSpeedGood { get; set; }

            /// <summary>
            /// Initialize to null.
            /// </summary>
            public GpsData()
            {
                GPVTG = null;
                GPHDT = null;
                GPGGA = null;
                GPRMC = null;
                BackupShipEast = 0.0;
                BackupShipNorth = 0.0;
                IsBackShipSpeedGood = false;
            }
        }

        /// <summary>
        /// Heading, Pitch and Roll values.
        /// </summary>
        public struct HPR
        {
            /// <summary>
            /// Heading.
            /// </summary>
            public double Heading { get; set; }

            /// <summary>
            /// Pitch.
            /// </summary>
            public double Pitch { get; set; }

            /// <summary>
            /// Roll.
            /// </summary>
            public double Roll { get; set; }
        }

        #endregion


        /// <summary>
        /// Mark the given data bad based off the range bin.  The value 
        /// will be set to BAD_VALUE.
        /// </summary>
        /// <param name="data">Data to mark bad below bottom.</param>
        /// <param name="rangeBin">Bin to set bad.</param>
        /// <param name="BAD_VALUE">BAD Value.</param>
        /// <returns>New array with the bottom marked bad.</returns>
        public static double[] MarkBadBelowBottom(double[] data, int rangeBin, double BAD_VALUE)
        {
            double[] result = new double[data.GetLength(0)];

            for(int x = 0; x < data.Length; x++)
            {
                if (x < rangeBin)
                {
                    // Use the original result
                    result[x] = data[x];
                }
                else
                {
                    // Mark bad below the bottom
                    result[x] = BAD_VALUE;
                }
            }

            return result;
        }


        #region Velocity Vector

        /// <summary>
        /// Create the velocity vectors and average values based off the reader.
        /// The reader should represent one ensemble from the database.
        /// 
        /// Query needs to include: EnsembleDS,AncillaryDS,BottomTrackDS,EarthVelocityDS
        /// 
        /// </summary>
        /// <param name="reader">Database reader for an ensemble.</param>
        /// <param name="backupBtEast">Backup value for Bottom Track East velocity value.</param>
        /// <param name="backupBtNorth">Backup value for Bottom Track North velocity value.</param>
        /// <param name="isMarkBadBelowBottom">Flag to mark the data below the bottom bad.</param>
        /// <param name="isRemoveShipSpeed">Remove the ship speed.</param>
        /// <returns>Velocity vector for the ensemble.</returns>
        public static VelocityMagDir CreateVelocityVectors(DbDataReader reader, double backupBtEast, double backupBtNorth, bool isRemoveShipSpeed = true, bool isMarkBadBelowBottom = true)
        {
            // Get the data as a JSON string
            string jsonEnsemble = reader["EnsembleDS"].ToString();
            string jsonBT = reader["BottomTrackDS"].ToString();
            string jsonAncillary = reader["AncillaryDS"].ToString();
            string jsonEarthVel = reader["EarthVelocityDS"].ToString();

            // Verify we have all the data
            if (string.IsNullOrEmpty(jsonEnsemble)  || string.IsNullOrEmpty(jsonAncillary) || string.IsNullOrEmpty(jsonEarthVel))
            {
                // No range found
                return null;
            }

            // Convert to JSON objects
            JObject ensData = JObject.Parse(jsonEnsemble);
            JObject ancData = JObject.Parse(jsonAncillary);
            JObject earthVelData = JObject.Parse(jsonEarthVel);

            // Get Bin Size and First bin
            double binSize = ancData["BinSize"].ToObject<double>();
            double firstBin = ancData["FirstBinRange"].ToObject<double>();
            int numBins = ensData["NumBins"].ToObject<int>();
            double[,] earthVel = earthVelData["EarthVelocityData"].ToObject<double[,]>();

            // Create the array for the result
            VelocityMagDir result = new VelocityMagDir(numBins);

            // Mark bad below bottom
            if (isMarkBadBelowBottom)
            {
                result.RangeBin = GetRangeBin(reader);
            }

            // Vertical beam
            if (earthVel.GetLength(1) == 1)
            {
                for (int bin = 0; bin < numBins; bin++)
                {
                    result.Magnitude[bin] = earthVel[bin, 0];
                }

                return result;
            }
            // 3 or 4 beam data
            else if (earthVel.GetLength(1) >= 2)
            {
                int count = 0;
                double accumMag = 0.0;
                double accumDirX = 0.0;
                double accumDirY = 0.0;

                for (int bin = 0; bin < numBins; bin++)
                {
                    // Mark bad below bottom
                    if (isMarkBadBelowBottom &&                 // Check if turned on
                        result.RangeBin != BAD_BOTTOM_BIN &&    // Verify a good range bin was found
                        bin >= result.RangeBin)                 // Check if this bin is below the bottom
                    {
                        result.Magnitude[bin] = BAD_VELOCITY;
                        result.DirectionXNorth[bin] = BAD_VELOCITY;
                        result.DirectionYNorth[bin] = BAD_VELOCITY;
                    }
                    else
                    {
                        // Get east and north velocity
                        double east = earthVel[bin, 0];
                        double north = earthVel[bin, 1];

                        // Verify good data
                        if (Math.Round(east, 3) == BAD_VELOCITY || Math.Round(north, 3) == BAD_VELOCITY)
                        {
                            result.Magnitude[bin] = BAD_VELOCITY;
                            result.DirectionXNorth[bin] = BAD_VELOCITY;
                            result.DirectionYNorth[bin] = BAD_VELOCITY;
                        }
                        else
                        {
                            // Remove the ship speed
                            if (isRemoveShipSpeed && !string.IsNullOrEmpty(jsonBT))
                            {
                                JObject btData = JObject.Parse(jsonBT);
                                double[] btEarthVel = btData["EarthVelocity"].ToObject<double[]>();

                                // Get the Bottom Track Earth North and East velocity
                                double btEast = 0.0;
                                double btNorth = 0.0;
                                if (Math.Round(btEarthVel[0], 3) != BAD_VELOCITY && Math.Round(btEarthVel[1], 3) != BAD_VELOCITY)
                                {
                                    // Store the values to use
                                    btEast = btEarthVel[0];
                                    btNorth = btEarthVel[1];

                                    // Store backup values
                                    result.IsBtVelGood = true;
                                    result.BtEastVel = btEast;
                                    result.BtNorthVel = btNorth;
                                }
                                else if(Math.Round(backupBtEast, 3) != BAD_VELOCITY && Math.Round(backupBtNorth, 3) != BAD_VELOCITY)
                                {
                                    // Use the back up value
                                    btEast = backupBtEast;
                                    btNorth = backupBtNorth;
                                }

                                // Remove the ship speed
                                east += btEast;
                                north += btNorth;
                            }

                            // Calculate magnitude and direction
                            result.Magnitude[bin] = Math.Abs(Math.Sqrt((east * east) + (north * north)));       // Magnitude
                            result.DirectionXNorth[bin] = CalcDirection(east, north);                           // Direction X axis North on the plot
                            result.DirectionYNorth[bin] = CalcDirection(north, east);                           // Direction Y axis North on the plot

                            // Accumulate the average
                            count++;
                            accumMag += result.Magnitude[bin];
                            accumDirX += result.DirectionXNorth[bin];
                            accumDirY += result.DirectionYNorth[bin];
                        }
                    }
                }

                // Calculate the average
                if (count > 0)
                {
                    result.AvgMagnitude = accumMag / count;
                    result.AvgDirectionXNorth = accumDirX / count;
                    result.AvgDirectionYNorth = accumDirY / count;
                }

                return result;
            }
            // Not enough beams
            else
            {
                return result;
            }
        }

        /// <summary>
        /// Calculate the Direction of the velocities given.
        /// Value will be returned in degrees.  Give the Y axis as the first parameter.
        /// Make the value between 0 and 360.
        /// 
        /// Parameters should be EAST,NORTH for water velocity in ADCP.
        /// 
        /// </summary>
        /// <param name="y">Y axis velocity value.</param>
        /// <param name="x">X axis velocity value.</param>
        /// <returns>Direction of the velocity return in degrees.</returns>
        public static double CalcDirection(double y, double x)
        {
            double dir = (Math.Atan2(y, x)) * (180.0 / Math.PI);

            // The range is -180 to 180
            // This moves it to 0 to 360
            if (dir < 0.0)
            {
                dir = 360.0 + dir;
            }

            return dir;
        }

        #endregion

        #region Heading Pitch and Roll

        /// <summary>
        /// Get the heading, pitch and roll value.
        /// </summary>
        /// <param name="reader">Reader of the database.</param>
        /// <returns>Heading, pitch and roll value.</returns>
        public static HPR GetHPR(DbDataReader reader)
        {
            string jsonAncillary = reader["AncillaryDS"].ToString();
            string jsonBt = reader["BottomTrackDS"].ToString();

            // Verify we have all the data
            if (string.IsNullOrEmpty(jsonAncillary) || string.IsNullOrEmpty(jsonBt) )
            {
                // No values found
                return new HPR()
                {
                    Heading = 0.0,
                    Pitch = 0.0,
                    Roll = 0.0
                };
            }

            // Convert to JSON objects
            JObject ancData = JObject.Parse(jsonAncillary);
            JObject btData = JObject.Parse(jsonBt);

            if (ancData != null && ancData.HasValues)
            {
                HPR hpr = new HPR();
                hpr.Heading = ancData["Heading"].ToObject<double>();
                hpr.Pitch = ancData["Pitch"].ToObject<double>();
                hpr.Roll = ancData["Roll"].ToObject<double>();

                return hpr;
            }
            else if(btData != null && btData.HasValues)
            {
                HPR hpr = new HPR();
                hpr.Heading = btData["Heading"].ToObject<double>();
                hpr.Pitch = btData["Pitch"].ToObject<double>();
                hpr.Roll = btData["Roll"].ToObject<double>();

                return hpr;
            }

            // No values found
            return new HPR()
            {
                Heading = 0.0,
                Pitch = 0.0,
                Roll = 0.0
            };
        }

        #endregion

        #region Range Bin

        /// <summary>
        /// Get the bin that represent the depth of the water.
        /// </summary>
        /// <param name="reader">Database reader.</param>
        /// <returns>Bin of the water depth.</returns>
        public static int GetRangeBin(DbDataReader reader)
        {
            try
            {
                // Get the data as a JSON string
                string jsonEnsemble = reader["EnsembleDS"].ToString();
                string jsonAncillary = reader["AncillaryDS"].ToString();

                if (string.IsNullOrEmpty(jsonEnsemble) || string.IsNullOrEmpty(jsonAncillary))
                {
                    // No range found
                    return BAD_BOTTOM_BIN;
                }

                // Convert to JSON objects
                JObject ensData = JObject.Parse(jsonEnsemble);
                JObject ancData = JObject.Parse(jsonAncillary);

                // Get Bin Size and First bin
                double binSize = ancData["BinSize"].ToObject<double>();
                double firstBin = ancData["FirstBinRange"].ToObject<double>();
                int numBins = ensData["NumBins"].ToObject<int>();

                int bin = 0;

                // Calculate the average Range
                double avgRange = GetAverageRange(reader);

                // Verify we found good range
                if (avgRange > 0)
                {
                    // Remove the Blank distance
                    avgRange -= firstBin;

                    if (avgRange > 0.0)
                    {
                        // Divide by the bin size and round to int
                        double binDepth = avgRange / binSize;

                        // Set the Bottom Bin
                        int bottomBin = (int)Math.Round(binDepth);

                        return bottomBin;
                    }
                }
                else
                {
                    // No range found and no backup
                    return BAD_BOTTOM_BIN;
                }

                return bin;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error getting the Range bin.", e);
                return BAD_BOTTOM_BIN;
            }
        }

        #endregion

        #region Average Range

        /// <summary>
        /// Get the average range.
        /// </summary>
        /// <param name="reader">Database reader.</param>
        /// <returns>Average range.  0 if not found.</returns>
        public static double GetAverageRange(DbDataReader reader)
        {
            try
            {
                // Get the data as a JSON string
                string jsonEnsemble = reader["EnsembleDS"].ToString();
                string jsonBT = reader["BottomTrackDS"].ToString();
                string jsonAncillary = reader["AncillaryDS"].ToString();

                if (string.IsNullOrEmpty(jsonEnsemble) || string.IsNullOrEmpty(jsonBT) || string.IsNullOrEmpty(jsonAncillary))
                {
                    // No range found
                    return BAD_BOTTOM_BIN;
                }

                // Convert to JSON objects
                JObject ensData = JObject.Parse(jsonEnsemble);
                JObject ancData = JObject.Parse(jsonAncillary);
                JObject btData = JObject.Parse(jsonBT);

                // Get Bin Size and First bin
                double binSize = ancData["BinSize"].ToObject<double>();
                double firstBin = ancData["FirstBinRange"].ToObject<double>();
                int numBins = ensData["NumBins"].ToObject<int>();

                // Get Bottom Track Ranges
                double[] ranges = btData["Range"].ToObject<double[]>();

                // Get the average range
                double avg = 0.0;
                int avgCt = 0;
                foreach (var range in ranges)
                {
                    if (range > 0.0)
                    {
                        avg += range;
                        avgCt++;
                    }
                }

                // Verify we found good range
                if (avgCt > 0)
                {
                    // Calculate the average Range
                    return avg / avgCt;
                }
                else
                {
                    return 0.0;
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("Error getting the average range.", e);
                return 0.0;
            }
        }

        #endregion

        #region Average Magnitude

        /// <summary>
        /// Get the average magnitude from the earth velocity vectors data.
        /// </summary>
        /// <param name="ensEarth">Earth JSON object.</param>
        /// <param name="isMarkBadBelowBottom">Flag if should mark bad below bottom.</param>
        /// <param name="rangeBin">Depth of the bin.</param>
        /// <param name="BAD_VALUE">Bad value to use.</param>
        /// <returns>Average of the magnitude data.</returns>
        public static double GetAvgMaggg(JObject ensEarth, bool isMarkBadBelowBottom, int rangeBin, double BAD_VALUE = BAD_VELOCITY)
        {
            double avg = 0.0;
            int count = 0;

            // Get the number of bins
            int numBins = ensEarth["NumElements"].ToObject<int>();
            for (int bin = 0; bin < numBins; bin++)
            {
                // Stop processing data if Mark Bad Below bottom
                if (isMarkBadBelowBottom && bin > rangeBin)
                {
                    break;
                }

                // Get the velocity vector magntidue from the JSON object and add it to the array
                double data = ensEarth["VelocityVectors"][bin]["Magnitude"].ToObject<double>();

                // Verify its good data
                if (Math.Round(data, 3) != BAD_VALUE)
                {
                    avg += data;
                    count++;
                }
            }

            // Take the average
            if (count > 0)
            {
                return avg / count;
            }

            return avg;
        }

        #endregion

        #region Average Direction

        /// <summary>
        /// Get the average direction from the earth velocity vector data.
        /// </summary>
        /// <param name="ensEarth">Earth JSON object.</param>
        /// <param name="isMarkBadBelowBottom">Flag if should mark bad below bottom.</param>
        /// <param name="rangeBin">Depth of the bin.</param>
        /// <param name="BAD_VALUE">Bad value to use.</param>
        /// <returns>Average of the direction data.</returns>
        public static double GetAvgDirrrr(JObject ensEarth, bool isMarkBadBelowBottom, int rangeBin, double BAD_VALUE = BAD_VELOCITY)
        {
            double avg = 0.0;
            int count = 0;

            // Get the number of bins
            int numBins = ensEarth["NumElements"].ToObject<int>();
            for (int bin = 0; bin < numBins; bin++)
            {
                // Stop processing data if Mark Bad Below bottom
                if (isMarkBadBelowBottom && bin > rangeBin)
                {
                    break;
                }

                // Get the velocity vector direction from the JSON object and add it to the array
                double data = ensEarth["VelocityVectors"][bin]["DirectionYNorth"].ToObject<double>();

                // Verify its good data
                if (Math.Round(data, 3) != BAD_VELOCITY)
                {
                    avg += data;
                    count++;
                }
            }

            // Take the average
            if (count > 0)
            {
                return avg / count;
            }

            return avg;
        }

        #endregion

        #region Decode GPS data

        /// <summary>
        /// Get the GPS data from the ensemble using the database reader.
        /// </summary>
        /// <param name="reader">Database reader.</param>
        /// <returns>If NMEA data exist, pass the data, or return NULL.</returns>
        public static GpsData GetGpsData(DbDataReader reader)
        {
            // Get the NMEA data
            string jsonNmea = reader["NmeaDS"].ToString();
            DbDataHelper.GpsData gpsData = null;
            if (!string.IsNullOrEmpty(jsonNmea))
            {
                // Convert to a JSON object
                JObject ensNmea = JObject.Parse(jsonNmea);
                string[] nmeaStrings = ensNmea["NmeaStrings"].ToObject<string[]>();

                if (nmeaStrings != null && nmeaStrings.Length > 0)
                {
                    gpsData = DbDataHelper.DecodeNmea(nmeaStrings);
                }
            }

            // Check if we have a valid GPS speed
            if (gpsData != null && gpsData.GPVTG != null && gpsData.GPHDT != null && gpsData.GPVTG.IsValid && gpsData.GPHDT.IsValid)
            {
                // Convert the speed and east and north component
                // Speed from the GPS
                double speed = gpsData.GPVTG.Speed.ToMetersPerSecond().Value;

                // Calculate the East and North component of the GPS speed
                gpsData.BackupShipEast = Convert.ToSingle(speed * Math.Sin(gpsData.GPHDT.Heading.ToRadians().Value));
                gpsData.BackupShipNorth = Convert.ToSingle(speed * Math.Cos(gpsData.GPHDT.Heading.ToRadians().Value));
                gpsData.IsBackShipSpeedGood = true;
            }
            else if (gpsData != null && gpsData.GPHDT != null && gpsData.GPRMC != null && gpsData.GPRMC.IsValid && gpsData.GPHDT.IsValid)
            {
                // Convert the speed and east and north component
                // Speed from the GPS
                double speed = gpsData.GPRMC.Speed.ToMetersPerSecond().Value;

                // Calculate the East and North component of the GPS speed
                gpsData.BackupShipEast = Convert.ToSingle(speed * Math.Sin(gpsData.GPHDT.Heading.ToRadians().Value));
                gpsData.BackupShipNorth = Convert.ToSingle(speed * Math.Cos(gpsData.GPHDT.Heading.ToRadians().Value));
                gpsData.IsBackShipSpeedGood = true;
            }

            return gpsData;
        }

        /// <summary>
        /// Decode the NMEA data.
        /// </summary>
        /// <param name="nmeaStrings">String array containing NMEA sentences.</param>
        /// <returns>Gps Data.</returns>
        public static GpsData DecodeNmea(string[] nmeaStrings)
        {
            GpsData gpsData = new GpsData();

            try
            {
                for(int x = 0; x < nmeaStrings.Length; x++)
                {
                    // Parse all the nmea setences found
                    NmeaSentence sentence = new NmeaSentence(nmeaStrings[x]);

                    // Is this a GPRMC sentence?
                    if (sentence.CommandWord.EndsWith("RMC", StringComparison.Ordinal))
                    {
                        gpsData.GPRMC = new GprmcSentence(sentence.Sentence);
                    }

                    // Is this a GPHDT sentence?
                    if (sentence.CommandWord.EndsWith("HDT", StringComparison.Ordinal))
                    {
                        gpsData.GPHDT = new GphdtSentence(sentence.Sentence);
                    }

                    // Is this a GPVTG sentence?
                    if (sentence.CommandWord.EndsWith("VTG", StringComparison.Ordinal))
                    {
                        gpsData.GPVTG = new GpvtgSentence(sentence.Sentence);
                    }

                    // Is this a GPGGA sentence?
                    if (sentence.CommandWord.EndsWith("GGA", StringComparison.Ordinal))
                    {
                        gpsData.GPGGA = new GpggaSentence(sentence.Sentence);
                    }

                    // Does this sentence support lat/long info?
                    IPositionSentence positionSentence = sentence as IPositionSentence;
                    if (positionSentence != null)
                    {
                        gpsData.Position = positionSentence.Position;
                    }

                    // Does this sentence support bearing?
                    IBearingSentence bearingSentence = sentence as IBearingSentence;
                    if (bearingSentence != null)
                    {
                        gpsData.Bearing = bearingSentence.Bearing;
                    }

                    // Does this sentence support heading?
                    IHeadingSentence headingSentence = sentence as IHeadingSentence;
                    if (headingSentence != null)
                    {
                        gpsData.Heading = headingSentence.Heading;
                    }

                    // Does this sentence support speed?
                    ISpeedSentence speedSentence = sentence as ISpeedSentence;
                    if (speedSentence != null)
                    {
                        gpsData.Speed = speedSentence.Speed;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error decoding GPS data.", e);
            }

            return gpsData;
        }

        #endregion
    }
}
