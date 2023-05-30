using System;

public class CoordinateConverter
{
    public static void Main()
    {
        // Test example values
        double easting = 500000;
        double northing = 200000;

        // Call the function and display the result
        Coordinate result = ConvertEastingNorthingToLatLong(easting, northing);
        Console.WriteLine("Latitude: " + result.Latitude);
        Console.WriteLine("Longitude: " + result.Longitude);
    }

    public static Coordinate ConvertEastingNorthingToLatLong(double easting, double northing)
    {
        // Constants for the calculation
        double a = 6377563.396; // Semi-major axis of the Airy 1830 ellipsoid
        double b = 6356256.909; // Semi-minor axis of the Airy 1830 ellipsoid
        double e0 = 400000; // Easting of false origin
        double n0 = -100000; // Northing of false origin
        double f0 = 0.9996012717; // Central meridian scale factor
        double phi0 = 49 * (Math.PI / 180); // Latitude of false origin in radians
        double lambda0 = -2 * (Math.PI / 180); // Longitude of false origin in radians

        // Calculate the required values
        double e2 = 1 - (b * b) / (a * a);
        double n = (a - b) / (a + b);
        double phiPrime = (northing - n0) / (a * f0) + phi0;

        // Iteratively calculate latitude until convergence
        double m;
        do
        {
            double phiTemp = phiPrime;
            double numerator = (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256) * phiTemp;
            double denominator = (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256);
            double term1 = (3 * n / 2 - 27 * n * n * n / 32) * Math.Sin(2 * phiTemp);
            double term2 = (21 * n * n / 16 - 55 * n * n * n * n / 32) * Math.Sin(4 * phiTemp);
            double term3 = (151 * n * n * n / 96) * Math.Sin(6 * phiTemp);
            double term4 = (1097 * n * n * n * n / 512) * Math.Sin(8 * phiTemp);
            phiPrime = numerator / denominator + term1 + term2 + term3 + term4;
            m = CalculateMeridianDistance(phiPrime, a, b, f0, phi0);
        } while (Math.Abs(northing - n0 - m) >= 0.00001); // Convergence condition

        // Calculate latitude, longitude, and scale factor
        double v = a * f0 * Math.Pow(1 - e2 * Math.Sin(phiPrime) * Math.Sin(phiPrime), -0.5);
        double rho = a * f0 * (1 - e2) * Math.Pow(1 - e2 * Math.Sin(phiPrime) * Math.Sin(phiPrime), -1.5);
        double eta2 = v / rho - 1;
        double VII = Math.Tan(phiPrime) / (2 * rho * v);
        double VIII = Math.Tan(phiPrime) / (24 * rho * Math.Pow(v, 3)) * (5 + 3 * Math.Tan(phiPrime) * Math.Tan(phiPrime) + eta2 - 9 * Math.Tan(phiPrime) * Math.Tan(phiPrime) * eta2);
        double IX = Math.Tan(phiPrime) / (720 * rho * Math.Pow(v, 5)) * (61 + 90 * Math.Tan(phiPrime) * Math.Tan(phiPrime) + 45 * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) - 252 * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) * eta2 - 3 * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) + 100 * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) * eta2 - 66 * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) * eta2 * eta2 - 90 * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) * eta2 * eta2 * eta2);
        double X = Math.Pow(Math.Cos(phiPrime), -1) / v;
        double XI = Math.Pow(Math.Cos(phiPrime), -1) / (6 * Math.Pow(v, 3)) * (v / rho + 2 * Math.Tan(phiPrime) * Math.Tan(phiPrime));
        double XII = Math.Pow(Math.Cos(phiPrime), -1) / (120 * Math.Pow(v, 5)) * (5 + 28 * Math.Tan(phiPrime) * Math.Tan(phiPrime) + 24 * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime));
        double XIIA = Math.Pow(Math.Cos(phiPrime), -1) / (5040 * Math.Pow(v, 7)) * (61 + 662 * Math.Tan(phiPrime) * Math.Tan(phiPrime) + 1320 * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) + 720 * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime) * Math.Tan(phiPrime));
        double phi = phiPrime - VII * Math.Pow(easting - e0, 2) + VIII * Math.Pow(easting - e0, 4) - IX * Math.Pow(easting - e0, 6);
        double lambda = lambda0 + X * (easting - e0) - XI * Math.Pow(easting - e0, 3) + XII * Math.Pow(easting - e0, 5) - XIIA * Math.Pow(easting - e0, 7);
        double scale = f0 * v * Math.Pow(1 - e2 * Math.Sin(phiPrime) * Math.Sin(phiPrime), 0.5);

        // Convert latitude and longitude to degrees
        double latitude = phi * (180 / Math.PI);
        double longitude = lambda * (180 / Math.PI);

        return new Coordinate(latitude, longitude);
    }

    public static double CalculateMeridianDistance(double phi, double a, double b, double f0, double phi0)
    {
        double n = (a - b) / (a + b);
        double n2 = n * n;
        double n3 = n * n * n;
        double n4 = n * n * n * n;

        double term1 = (1 + n + 1.25 * n2 + 1.25 * n3 + 1.875 * n4) * (phi - phi0);
        double term2 = (3 * n + 3 * n2 + 2.625 * n3 + 2.625 * n4) * Math.Sin(phi - phi0) * Math.Cos(phi + phi0);
        double term3 = (1.875 * n2 + 1.875 * n3 + 1.3125 * n4) * Math.Sin(2 * (phi - phi0)) * Math.Cos(2 * (phi + phi0));
        double term4 = (35 / 24) * n3 * Math.Sin(3 * (phi - phi0)) * Math.Cos(3 * (phi + phi0));
        double term5 = (35 / 24) * n4 * Math.Sin(4 * (phi - phi0)) * Math.Cos(4 * (phi + phi0));
        
        return b * f0 * (term1 - term2 + term3 - term4 + term5);
    }
}

public class Coordinate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}
