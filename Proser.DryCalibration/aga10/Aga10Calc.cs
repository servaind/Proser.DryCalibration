using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Proser.DryCalibration.aga10
{
    #region STRUCT

    public struct GasComponents
    {
        public double CH4;             // Metano
        public double N2;              // Nitrogeno
        public double CO2;             // Dioxido de carbono
        public double C2H6;            // Etano
        public double C3H8;            // Propano
        public double H2O;             // Agua
        public double H2S;             // Sulfhidrico
        public double H2;              // Hidrogeno
        public double CO;              // Monoxido de carbono
        public double O2;              // Oxigeno
        public double iC4H10;          // iso Butano
        public double nC4H10;          // normal Butano
        public double iC5H12;          // iso Pentano
        public double nC5H12;          // normal Pentano
        public double C6H14;           // Hexano
        public double C7H16;           // Heptano
        public double C8H18;           // Octano
        public double C9H20;           // Nonano
        public double C10H22;          // Decano
        public double HE;              // Helio
        public double AR;              // Argon

        public GasComponents(double CH4, double N2, double CO2, double C2H6, double C3H8,
                            double H2O, double H2S, double H2, double CO, double O2, double iC4H10,
                            double nC4H10, double iC5H12, double nC5H12, double C6H14,
                            double C7H16, double C8H18, double C9H20, double C10H22, double HE, double AR)
        {
            this.AR = AR;
            this.C10H22 = C10H22;
            this.C2H6 = C2H6;
            this.C3H8 = C3H8;
            this.C6H14 = C6H14;
            this.C7H16 = C7H16;
            this.C8H18 = C8H18;
            this.C9H20 = C9H20;
            this.CH4 = CH4;
            this.CO = CO;
            this.CO2 = CO2;
            this.H2 = H2;
            this.H2O = H2O;
            this.H2S = H2S;
            this.HE = HE;
            this.iC4H10 = iC4H10;
            this.iC5H12 = iC5H12;
            this.N2 = N2;
            this.nC4H10 = nC4H10;
            this.nC5H12 = nC5H12;
            this.O2 = O2;
        }
    }

    public struct aga8_defs
    {
        public double[] x;  // reordered gas compositions in fractions
        public double F;
        public double Q;
        public double G;
        public double U;
        public double K;
        public double[] bTerm;
        public double[] fn;
        public double[] sn;

        public aga8_defs(double[] x, double F, double Q, double G, double U, double K, double[] bTerm, double[] fn, double[] sn)
        {
            this.bTerm = bTerm;
            this.F = F;
            this.fn = fn;
            this.G = G;
            this.K = K;
            this.Q = Q;
            this.sn = sn;
            this.U = U;
            this.x = x;
        }
    }

    public struct stateParams_92
    {
        public double an;
        public byte bn;
        public byte cn;
        public byte kn;
        public double un;

        public stateParams_92(double an, byte bn, byte cn, byte kn, double un)
        {
            this.an = an;
            this.bn = bn;
            this.cn = cn;
            this.kn = kn;
            this.un = un;
        }
    }

    public struct charParams_92
    {
        public double E;
        public double K;      // K ^ 3
        public double G;

        public charParams_92(double E, double K, double G)
        {
            this.E = E;
            this.K = K;
            this.G = G;
        }
    }

    // Table 5 also lists Q, F, S and W params which are mostly 0 with
    //   seven exceptions. The exceptions are handled in the code.

    public struct heat_capacity
    {
        public double A;
        public double B;
        public double C;
        public double D;
        public double E;
        public double F;
        public double G;
        public double H;
        public double I;
        public double J;
        public double K;

        public heat_capacity(double A, double B, double C, double D, double E, double F,
                            double G, double H, double I, double J, double K)
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.D = D;
            this.E = E;
            this.F = F;
            this.G = G;
            this.H = H;
            this.I = I;
            this.J = J;
            this.K = K;
        }
    }


    #endregion

    public class Aga10Calc
    {
        //        public const double[] Molar_mass = null;    // Molar Mass, from table 5, aga8
        //        public const stateParams_92[] stateParams = null; // from table 4, P 22, aga8 92
        //        public const charParams_92[] charParams = null;  // from table 5, P 24, 92 aga8
        //        public const heat_capacity[] hccoeff = null;

        private const byte ZS = 0;
        private const byte ZB = 1;
        private const byte ZF = 2;
        private const double RGAS = 0.00831451; // Constante universal de los gases
        private const double rPI = 1.77245385; // Raiz cuadrada de pi
        private const double cvFact = 1.00050943;// Factor de corrección cv

        private aga8_defs aga8_92;
        public GasComponents gas_comp;

        public double[] Molar_mass = new double[21]
        { // Molar Mass, from table 5, aga8
              16.0430f,  //  Methane          CH4
              28.0135f,  //  Nitrogen          N2
              44.0100f,  //  Carbon dioxide   CO2
              30.0700f,  //  Ethane          C2H6
              44.0970f,  //  Propane         C3H8
              18.0153f,  //  Water            H2O
              34.0820f,  //  Hydrogen sulfide H2S
               2.0159f,  //  Hydrogen          H2
              28.0100f,  //  Carbon monoxide   CO
              31.9988f,  //  Oxygen            O2
              58.1230f,  //  iso-butane     C4H10
              58.1230f,  //  n-butane       C4H10
              72.1500f,  //  iso-pentane    C5H12
              72.1500f,  //  n-pentane      C5H12
              86.1770f,  //  n-hexane       C6H14
             100.2040f,  //  n-heptane      C7H16
             114.2310f,  //  n-octane       C8H18
             128.2580f,  //  n-nonane       C9H20
             142.2850f,  //  n-decane      C10H22
               4.0026f,  //  Helium            He
              39.9480f   //  Argon             Ar
        };

        public stateParams_92[] stateParams = // from table 4, P 22, aga8 92
        {
            new stateParams_92(  0.153832600, 1, 0, 0,   0.0), //  1
            new stateParams_92(  1.341953000, 1, 0, 0,   0.5), //  2
            new stateParams_92( -2.998583000, 1, 0, 0,   1.0), //  3
            new stateParams_92( -0.048312280, 1, 0, 0,   3.5), //  4
            new stateParams_92(  0.375796500, 1, 0, 0,  -0.5), //  5
            new stateParams_92( -1.589575000, 1, 0, 0,   4.5), //  6
            new stateParams_92( -0.053588470, 1, 0, 0,   0.5), //  7
            new stateParams_92(  0.886594630, 1, 0, 0,   7.5), //  8
            new stateParams_92( -0.710237040, 1, 0, 0,   9.5), //  9
            new stateParams_92( -1.471722000, 1, 0, 0,   6.0), // 10
            new stateParams_92(  1.321850350, 1, 0, 0,  12.0), // 11
            new stateParams_92( -0.786659250, 1, 0, 0,  12.5), // 12
            new stateParams_92(  2.29129E-08, 1, 1, 3,  -6.0), // 13
            new stateParams_92(  0.157672400, 1, 1, 2,   2.0), // 14
            new stateParams_92( -0.436386400, 1, 1, 2,   3.0), // 15
            new stateParams_92( -0.044081590, 1, 1, 2,   2.0), // 16
            new stateParams_92( -0.003433888, 1, 1, 4,   2.0), // 17
            new stateParams_92(  0.032059050, 1, 1, 4,  11.0), // 18
            new stateParams_92(  0.024873550, 2, 0, 0,  -0.5), // 19
            new stateParams_92(  0.073322790, 2, 0, 0,   0.5), // 20
            new stateParams_92( -0.001600573, 2, 1, 2,   0.0), // 21
            new stateParams_92(  0.642470600, 2, 1, 2,   4.0), // 22
            new stateParams_92( -0.416260100, 2, 1, 2,   6.0), // 23
            new stateParams_92( -0.066899570, 2, 1, 4,  21.0), // 24
            new stateParams_92(  0.279179500, 2, 1, 4,  23.0), // 25
            new stateParams_92( -0.696605100, 2, 1, 4,  22.0), // 26
            new stateParams_92( -0.002860589, 2, 1, 4,  -1.0), // 27
            new stateParams_92( -0.008098836, 3, 0, 0,  -0.5), // 28
            new stateParams_92(  3.150547000, 3, 1, 1,   7.0), // 29
            new stateParams_92(  0.007224479, 3, 1, 1,  -1.0), // 30
            new stateParams_92( -0.705752900, 3, 1, 2,   6.0), // 31
            new stateParams_92(  0.534979200, 3, 1, 2,   4.0), // 32
            new stateParams_92( -0.079314910, 3, 1, 3,   1.0), // 33
            new stateParams_92( -1.418465000, 3, 1, 3,   9.0), // 34
            new stateParams_92( -5.99905E-17, 3, 1, 4, -13.0), // 35
            new stateParams_92(  0.105840200, 3, 1, 4,  21.0), // 36
            new stateParams_92(  0.034317290, 3, 1, 4,   8.0), // 37
            new stateParams_92( -0.007022847, 4, 0, 0,  -0.5), // 38
            new stateParams_92(  0.024955870, 4, 0, 0,   0.0), // 39
            new stateParams_92(  0.042968180, 4, 1, 2,   2.0), // 40
            new stateParams_92(  0.746545300, 4, 1, 2,   7.0), // 41
            new stateParams_92( -0.291961300, 4, 1, 2,   9.0), // 42
            new stateParams_92(  7.294616000, 4, 1, 4,  22.0), // 43
            new stateParams_92( -9.936757000, 4, 1, 4,  23.0), // 44
            new stateParams_92( -0.005399808, 5, 0, 0,   1.0), // 45
            new stateParams_92( -0.243256700, 5, 1, 2,   9.0), // 46
            new stateParams_92(  0.049870160, 5, 1, 2,   3.0), // 47
            new stateParams_92(  0.003733797, 5, 1, 4,   8.0), // 48
            new stateParams_92(  1.874951000, 5, 1, 4,  23.0), // 49
            new stateParams_92(  0.002168144, 6, 0, 0,   1.5), // 50
            new stateParams_92( -0.658716400, 6, 1, 2,   5.0), // 51
            new stateParams_92(  0.000205518, 7, 0, 0,  -0.5), // 52
            new stateParams_92(  0.009776195, 7, 1, 2,   4.0), // 53
            new stateParams_92( -0.020487080, 8, 1, 1,   7.0), // 54
            new stateParams_92(  0.015573220, 8, 1, 2,   3.0), // 55
            new stateParams_92(  0.006862415, 8, 1, 2,   0.0), // 56
            new stateParams_92( -0.001226752, 9, 1, 2,   1.0), // 57
            new stateParams_92(  0.002850908, 9, 1, 2,   0.0)  // 58
        };

        public charParams_92[] charParams =  // from table 5, P 24, 92 aga8
        {
             new charParams_92(  151.3182983,  0.4619255,  0.0000000 ), //  0 = Methane
             new charParams_92(   99.7377777,  0.4479153,  0.0278150 ), //  1 = Nitrogen
             new charParams_92(  241.9606018,  0.4557489,  0.1890650 ), //  2 = Carbon Dioxide
             new charParams_92(  244.1667023,  0.5279209,  0.0793000 ), //  3 = Ethane
             new charParams_92(  298.1182861,  0.5837490,  0.1412390 ), //  4 = Propane
             new charParams_92(  514.0156250,  0.3825868,  0.3325000 ), //  5 = Water
             new charParams_92(  296.3550110,  0.4618263,  0.0885000 ), //  6 = Hydrogen Sulfide
             new charParams_92(   26.9579391,  0.3514916,  0.0343690 ), //  7 = Hydrogen
             new charParams_92(  105.5347977,  0.4533894,  0.0389530 ), //  8 = Carbon Monoxide
             new charParams_92(  122.7667007,  0.4186954,  0.0210000 ), //  9 = Oxygen
             new charParams_92(  324.0689087,  0.6406937,  0.2566920 ), // 10 = i-Butane
             new charParams_92(  337.6388855,  0.6341423,  0.2818350 ), // 11 = n-Butane
             new charParams_92(  365.5999146,  0.6738577,  0.3322670 ), // 12 = i-Pentane
             new charParams_92(  370.6823120,  0.6798307,  0.3669110 ), // 13 = n-Pentane
             new charParams_92(  402.6362915,  0.7175118,  0.2897310 ), // 14 = n-Hexane
             new charParams_92(  427.7226257,  0.7525189,  0.3375420 ), // 15 = n-Heptane
             new charParams_92(  450.3250122,  0.7849550,  0.3833810 ), // 16 = n-Octane
             new charParams_92(  470.8408813,  0.8152731,  0.4273540 ), // 17 = n-Nonane
             new charParams_92(  489.5583801,  0.8437826,  0.4696590 ), // 18 = n-Decane
             new charParams_92(    2.6101110,  0.3589888,  0.0000000 ), // 19 = Helium
             new charParams_92(  119.6299000,  0.4216551,  0.0000000 ) // 20 = Argon
        };

        public heat_capacity[] hccoeff =
        {
             new heat_capacity( -29776.4, 7.95454,  43.9417, 1037.09,   1.56373,  813.205, -24.9027, 1019.98, -10.1601, 1070.14, -20.0615), // Metano
             new heat_capacity( -3495.34, 6.95587, 0.272892, 662.738, -0.291318, -680.562,  1.78980, 1740.06,      0.0,  100.00,  4.49823), // Nitrogeno
             new heat_capacity(  20.7307, 6.96237,  2.68645, 500.371,  -2.56429, -530.443,  3.91921, 500.198,  2.13290, 2197.22,  5.81381), // Dioxido de carbono
             new heat_capacity( -37524.4, 7.98139,  24.3668, 752.320,   3.53990,  272.846,  8.44724, 1020.13, -13.2732, 869.510, -22.4010), // Etano
             new heat_capacity( -56072.1, 8.14319,  37.0629, 735.402,   9.38159,  247.190,  13.4556, 1454.78, -11.7342, 984.518, -24.0426), // Propano
             new heat_capacity( -13773.1, 7.97183,  6.27078, 2572.63,   2.05010,  1156.72,      0.0,   100.0,      0.0,   100.0, -3.24989), // Agua
             new heat_capacity( -10085.4, 7.94680, -0.08380, 433.801,   2.85539,  843.792,  6.31595, 1481.43, -2.88457, 1102.23, -0.51551), // Sulfhidrico
             new heat_capacity( -5565.60, 6.66789,  2.33458, 2584.98,  0.749019,  559.656,      0.0,   100.0,      0.0,   100.0, -7.94821), // Hidrogeno
             new heat_capacity( -2753.49, 6.95854,  2.02441, 1541.22,  0.096774,  3674.81,      0.0,   100.0,      0.0,   100.0,  6.23387), // Monoxido de carbono
             new heat_capacity( -3497.45, 6.96302,  2.40013, 2522.05,   2.21752,  1154.15,      0.0,   100.0,      0.0,   100.0,  9.19749), // Oxigeno
             new heat_capacity( -72387.0, 17.8143,  58.2063, 1787.39,   40.7621,  808.645,      0.0,   100.0,      0.0,   100.0, -44.1341), // iso Butano
             new heat_capacity( -72674.8, 18.6383,  57.4178, 1792.73,   38.6599,  814.151,      0.0,   100.0,      0.0,   100.0, -46.1938), // normal Butano
             new heat_capacity( -91505.5, 21.3861,  74.3410, 1701.58,   47.0587,  759.600,      0.0,   100.0,      0.0,   100.0, -73.0337), // iso Pentano
             new heat_capacity( -83845.2, 22.5012,  69.5789, 1719.58,   46.2164,  802.174,      0.0,   100.0,      0.0,   100.0, -62.2197), // normal Pentano
             new heat_capacity( -94982.5, 26.6225,  80.3819, 1718.49,   55.6598,  802.069,      0.0,   100.0,      0.0,   100.0, -77.5366), // Hexano
             new heat_capacity(-103353.0, 30.4029,  90.6941, 1669.32,   63.2028,  786.001,      0.0,   100.0,      0.0,   100.0, -92.0164), // Heptano
             new heat_capacity(-109674.0, 34.0847,  100.253, 1611.55,   69.7675,  768.847,      0.0,   100.0,      0.0,   100.0, -106.149), // Octano
             new heat_capacity(-122599.0, 38.5014,  111.446, 1646.48,   80.5015,  781.588,      0.0,   100.0,      0.0,   100.0, -122.444), // Nonano
             new heat_capacity(-133564.0, 42.7143,  122.173, 1654.85,   90.2255,  785.564,      0.0,   100.0,      0.0,   100.0, -138.006), // Decano
             new heat_capacity(  1481.12, 4.96797,      0.0,     0.0,       0.0,      0.0,      0.0,   100.0,      0.0,   100.0,  30.1513), // Helio
             new heat_capacity(  1481.12, 4.96797,      0.0,     0.0,       0.0,      0.0,      0.0,   100.0,      0.0,   100.0,  37.0080)  // Argon
        };

        public double Temp, Pres, Prsr, new_temp, new_prsr, new_Z;
        private double Tf, Tb, Pf, Pb, Zb, Zs, Zf, Cr;
        private double coeff_B, dBdT, d2BdT2, density, Mr, c0p;
        private double code, rho, rho1, rho2, rhomax, p1, p2;
        private double videal, del, rhol, rhoh, prhol, prhoh;
        private double[] qi, hi, di, mi;
        private double dZdT, d2ZdT2, dZdP, cv, cp;
        private double[] A, B;

        public Aga10Calc()
        {
            A = new double[58];
            B = new double[58];
            qi = new double[21];
            hi = new double[21];
            di = new double[21];
            mi = new double[21];

            aga8_92 = new aga8_defs();
            aga8_92.bTerm = new double[18];
            aga8_92.fn = new double[58];
            aga8_92.sn = new double[58];
            aga8_92.x = new double[21];
            gas_comp = new GasComponents();
        }

        public double getEij(uint i, uint j)
        {
            // get binary interaction param Eij from table 6, aga 8 92
            switch (100 * i + j)
            {
                case 1: return (0.9716400);
                case 2: return (0.9606440);
                case 4: return (0.9946350);
                case 5: return (0.7082180);
                case 6: return (0.9314840);
                case 7: return (1.1705199);
                case 8: return (0.9901260);
                case 10: return (1.0195301);
                case 11: return (0.9898440);
                case 12: return (1.0023500);
                case 13: return (0.9992680);
                case 14: return (1.1072741);
                case 15: return (0.8808800);
                case 16: return (0.8809730);
                case 17: return (0.8810670);
                case 18: return (0.8811610);
                case 102: return (1.0227400);
                case 103: return (0.9701200);
                case 104: return (0.9459390);
                case 105: return (0.7469540);
                case 106: return (0.9022710);
                case 107: return (1.0863200);
                case 108: return (1.0057100);
                case 109: return (1.0210000);
                case 110: return (0.9469140);
                case 111: return (0.9733840);
                case 112: return (0.9593400);
                case 113: return (0.9455200);
                case 203: return (0.9250530);
                case 204: return (0.9602370);
                case 205: return (0.8494080);
                case 206: return (0.9550520);
                case 207: return (1.2817900);
                case 208: return (1.5000000);
                case 210: return (0.9068490);
                case 211: return (0.8973620);
                case 212: return (0.7262550);
                case 213: return (0.8597640);
                case 214: return (0.8551340);
                case 215: return (0.8312290);
                case 216: return (0.8083100);
                case 217: return (0.7863230);
                case 218: return (0.7651710);
                case 304: return (1.0225600);
                case 305: return (0.6931680);
                case 306: return (0.9468710);
                case 307: return (1.1644599);
                case 311: return (1.0130600);
                case 313: return (1.0053200);
                case 407: return (1.0347871);
                case 411: return (1.0049000);
                case 614: return (1.0086920);
                case 615: return (1.0101260);
                case 616: return (1.0115010);
                case 617: return (1.0128210);
                case 618: return (1.0140890);
                case 708: return (1.1000000);
                case 710: return (1.3000000);
                case 711: return (1.3000000);
                default: return (1.0000000);
            }

        } // getEij

        public double getUij(uint i, uint j)
        {
            // get binary interaction param Uij from table 6, aga 8 92
            switch (100 * i + j)
            {
                case 1: return (0.8861060);
                case 2: return (0.9638270);
                case 4: return (0.9908770);
                case 6: return (0.7368330);
                case 7: return (1.1563900);
                case 11: return (0.9922910);
                case 13: return (1.0036700);
                case 14: return (1.3025759);
                case 15: return (1.1919039);
                case 16: return (1.2057689);
                case 17: return (1.2196341);
                case 18: return (1.2334980);
                case 102: return (0.8350580);
                case 103: return (0.8164310);
                case 104: return (0.9155020);
                case 106: return (0.9934760);
                case 107: return (0.4088380);
                case 111: return (0.9935560);
                case 203: return (0.9698700);
                case 206: return (1.0452900);
                case 208: return (0.9000000);
                case 214: return (1.0666380);
                case 215: return (1.0776340);
                case 216: return (1.0881780);
                case 217: return (1.0982910);
                case 218: return (1.1080210);
                case 304: return (1.0651730);
                case 306: return (0.9719260);
                case 307: return (1.6166600);
                case 310: return (1.2500000);
                case 311: return (1.2500000);
                case 312: return (1.2500000);
                case 313: return (1.2500000);
                case 614: return (1.0289730);
                case 615: return (1.0337540);
                case 616: return (1.0383379);
                case 617: return (1.0427350);
                case 618: return (1.0469660);
                default: return (1.0000000);
            }
        } // getUij

        public double getKij(uint i, uint j)
        {
            // get binary interaction param Kij from table 6, aga 8 92
            switch (100 * i + j)
            {
                case 1: return (1.0036300);
                case 2: return (0.9959330);
                case 4: return (1.0076190);
                case 6: return (1.0000800);
                case 7: return (1.0232600);
                case 11: return (0.9975960);
                case 13: return (1.0025290);
                case 14: return (0.9829620);
                case 15: return (0.9835650);
                case 16: return (0.9827070);
                case 17: return (0.9818490);
                case 18: return (0.9809910);
                case 102: return (0.9823610);
                case 103: return (1.0079600);
                case 106: return (0.9425960);
                case 107: return (1.0322700);
                case 203: return (1.0085100);
                case 206: return (1.0077900);
                case 214: return (0.9101830);
                case 215: return (0.8953620);
                case 216: return (0.8811520);
                case 217: return (0.8675200);
                case 218: return (0.8544060);
                case 304: return (0.9868930);
                case 306: return (0.9999690);
                case 307: return (1.0203400);
                case 614: return (0.9681300);
                case 615: return (0.9628700);
                case 616: return (0.9578280);
                case 617: return (0.9524410);
                case 618: return (0.9483380);
                default: return (1.0000000);
            }

        } // getKij

        public double getGij(uint i, uint j)
        {
            // get binary interaction param Gij from table 6, aga 8 92
            switch (100 * i + j)
            {
                case 2: return (0.8076530);
                case 7: return (1.9573100);
                case 102: return (0.9827460);
                case 203: return (0.3702960);
                case 205: return (1.6730900);
                default: return (1.0000000);
            }

        } // getGij
        //**************************************************************************

        public void aga8_table(byte Z_flag)
        {
            // Task for computing compressibility factor for given temp and prsr as per aga8.

            // Calc binary interaction coeffs for temp in new_temp.

            ZeqnCoeffsForGivenTemp();

            // For the coeffs for tempSet, compute Z for new_prsr

            if (Z_flag == ZF) ZForGivenTempAndPrsr();
            switch (Z_flag)
            {
                case ZB:
                    Zb = 1 + coeff_B * Pb * 6.894757E-03 / (RGAS * (Tb + 459.67) / 1.8);

                    if (Temp != 519.67)
                    {
                        new_temp = 519.67;
                        ZeqnCoeffsForGivenTemp();
                        Temp = 519.67;
                    }
                    Prsr = 14.73;
                    Zs = 1 + coeff_B * 14.73 * 6.894757E-03 / (RGAS * 519.67 / 1.8);
                    break;

                case ZF:
                    Zf = new_Z;
                    break;
            }
        }// aga8_table ()
        //****************************************************************************

        public void ZeqnCoeffsForGivenTemp()
        {
            double tempInKelvin;
            byte i;

            tempInKelvin = new_temp / 1.80;

            // calculate B, second virial coeff, as per eq 15, P 18, aga8 92
            coeff_B = 0;
            dBdT = 0;
            d2BdT2 = 0;

            for (i = 0; i < 18; i++)
            {
                coeff_B += aga8_92.bTerm[i] / Math.Pow(tempInKelvin, stateParams[i].un);
                dBdT += aga8_92.bTerm[i] * stateParams[i].un /
                           Math.Pow(tempInKelvin, (stateParams[i].un + 1));
                d2BdT2 += aga8_92.bTerm[i] * stateParams[i].un * (stateParams[i].un + 1) /
                           Math.Pow(tempInKelvin, (stateParams[i].un + 2));
            }
            dBdT *= (-1.0);       // Cambio de signo de la sumatoria
            calcfn(tempInKelvin); // Calcular Cn*(13) a Cn*(58)
        } // ZeqnCoeffsForGivenTemp
        //**************************************************************************

        public void ZForGivenTempAndPrsr()
        {
            double Z;

            density = ddetail();
            Z = ZforGivenTempAndDensity(density);
            new_Z = Z;

        }  // ZForGivenTempAndPrsr
        //**************************************************************************

        public double ZforGivenTempAndDensity(double d)
        {
            byte i;
            double Z;

            calcsn(d);
            Z = 0;
            for (i = 12; i < 58; i++) Z += aga8_92.fn[i] * aga8_92.sn[i];
            return (Z + 1.0 + coeff_B * d);
        } // ZforGivenTempAndDensity
        //**************************************************************************

        public double pdetail(double d)
        {
            // this function calculates the pressure from the 1992 aga8 model
            // as a function of density and temperature.
            //
            //    description of arguments:
            //      d          density in mol/dm^3. (Input)
            //      t          temperature in kelvins. (Input)
            //      pdetail    pressure in mpa. (Output)

            double Z, p;

            Z = ZforGivenTempAndDensity(d);
            p = Z * d * RGAS * new_temp / 1.8;

            return p;

        } // pdetail ()
        //***************************************************************************

        public void braket()
        {
            // subroutine to bracket density solution for the 1992 aga8 model

            uint imax, it;
            double t, p;

            p = 6.894757E-03 * new_prsr;
            t = new_temp / 1.80;

            code = 0;
            imax = 200;

            rho1 = 0.0;
            p1 = 0.0;
            rhomax = 1.0 / aga8_92.K;
            if (t > (1.2593 * aga8_92.U))
                rhomax = 20.0 * rhomax;
            videal = RGAS * t / p;
            if ((Math.Abs(coeff_B)) < (0.167 * videal))
                rho2 = 0.95 / (videal + coeff_B);
            else
                rho2 = 1.15 / videal;

            //note: pressure (p2) at density rho2 not yet calculated

            del = rho2 / 20.0;
            it = 0;

            // start iterative density search loop

            while (true)
            {
                it = it + 1;

                if (it > imax)
                {
                    // maximum number of iterations exceeded
                    code = 3;
                    // write (*, 1010)
                    rho = rho2;
                    return;
                }

                if ((code != 2) && (rho2 > rhomax))
                {
                    // density in braket exceeds maximum allowable density
                    code = 2;
                    // write (*, 1020)
                    del = 0.01 * (rhomax - rho1) + p / (RGAS * t) / 20.0;
                    rho2 = rho1 + del;
                    continue;
                }

                // calculate pressure p2 at density rho2

                p2 = pdetail(rho2);
                // test value of p2 relative to p and relative to p1

                if (p2 > p)
                {
                    // the density root is bracketed (p1<p and p2>p)
                    rhol = rho1;
                    prhol = p1;
                    rhoh = rho2;
                    prhoh = p2;
                    return;
                }
                else
                {
                    if ((p2 > p1) && (code == 2))
                    {
                        // retain constant value for del (code=2)
                        rho1 = rho2;
                        p1 = p2;
                        rho2 = rho1 + del;
                        continue;
                    }

                    if ((p2 > p1) && (code == 0))
                    {
                        // increase value for del (code=0)
                        del = 2.0 * del;
                        rho1 = rho2;
                        p1 = p2;
                        rho2 = rho1 + del;
                        continue;
                    }
                }

                code = 1;

                rho = rho1;
                return;
            } // while ()
        } // braket ()
        //****************************************************************************

        public double ddetail()
        {
            // this function calculates density for the aga8 model given
            // pressure and temperature.  This function uses brent's method
            // and pdetail to determine the density.
            //
            // description of arguments:
            //  p        pressure in mpa. (Input)
            //  t        temperature in kelvins. (Input)
            //  ddetail density at p and t in mol/dm3. (Output)
            //
            //      version 11-18-92

            uint imax, i;

            double p, t;
            double epsp, epsr, epsmin;
            double x1, x2, x3, y1, y2, y3, y2my3, y3my1, y1my2;
            double delx, delprv, delmin, delbis, xnumer, xdenom, sgndel;
            double boundn;

            imax = 150;
            epsp = 1.0E-06;
            epsr = 1.0E-06;
            epsmin = 1.0E-07;
            code = 0;

            p = 6.894757E-03 * new_prsr;
            t = new_temp / 1.80;

            // call subroutine braket to bracket density solution

            braket();

            // check value of "code" returned from subroutine braket

            if ((code == 1) || (code == 3))
                return rho;

            // set up to start brent's method
            // x is the independent variable, y the dependent variable
            // delx is the current iteration change in x
            // delprv is the previous iteration change in x

            x1 = rhol;
            x2 = rhoh;
            y1 = prhol - p;
            y2 = prhoh - p;
            delx = x1 - x2;
            delprv = delx;

            // note that solution is bracketed between x1 and x2
            // a third point x3 is introduced for quadratic interpolation

            x3 = x1;
            y3 = y1;

            for (i = 1; i <= imax; i++)
            {
                // y3 must be opp in sign from y2, so solution between x2, x3

                if ((y2 * y3) > 0.0)
                {
                    x3 = x1;
                    y3 = y1;
                    delx = x1 - x2;
                    delprv = delx;
                }

                // y2 must be value of y closest to y=0.0, then x2new=x2old+delx

                if ((Math.Abs(y3)) < (Math.Abs(y2)))
                {
                    x1 = x2;
                    x2 = x3;
                    x3 = x1;
                    y1 = y2;
                    y2 = y3;
                    y3 = y1;
                }

                // delmin is minimum allowed step size for unconverged iteration
                //  smaller steps than delmin can cause oscillations

                delmin = epsmin * (Math.Abs(x2));

                // if procedure is not converging, i.e., abs(y2)>abs(y1),
                // or if delprv is less than delmin, i.e., interpolation
                // is converging too slowly, use bisection
                // delbis = 0.5d0*(x3 - x2) is the bisection delx

                delbis = 0.5 * (x3 - x2);

                // tests to decide numerical method for current iteration

                if (((Math.Abs(delprv)) < delmin) || ((Math.Abs(y1)) < (Math.Abs(y2))))
                {
                    // use bisection

                    delx = delbis;
                    delprv = delbis;
                }
                else
                {
                    if (x3 != x1)
                    {
                        // use inverse quadratic interpolation

                        y2my3 = y2 - y3;
                        y3my1 = y3 - y1;
                        y1my2 = y1 - y2;
                        xdenom = -(y1my2) * (y2my3) * (y3my1);
                        xnumer = x1 * y2 * y3 * (y2my3) + x2 * y3 * y1 * (y3my1) +
                         x3 * y1 * y2 * (y1my2) - x2 * xdenom;
                    }
                    else
                    {
                        // use inverse linear interpolation

                        xnumer = (x2 - x1) * y2;
                        xdenom = y1 - y2;
                    }

                    // following brent's procedure, before calculating delx,
                    // check that delx=xnumer/xdenom does not step out of bounds
                    if ((2.0 * (Math.Abs(xnumer))) < (Math.Abs(delprv * xdenom)))
                    {
                        // procedure converging, use interpolation

                        delprv = delx;
                        delx = xnumer / xdenom;
                    }
                    else
                    {
                        // procedure diverging, use bisection

                        delx = delbis;
                        delprv = delbis;
                    }
                }

                // check for convergence

                if (((Math.Abs(y2)) < (epsp * p)) && ((Math.Abs(delx)) < (epsr * (Math.Abs(x2)))))
                {
                    return (x2 + delx);
                }
                // when unconverged, abs(delx) must be greater than delmin
                // minimum allowed magnitude of change in x2 is 1.0000009*delmin
                // sgndel, the sign of change in x2 is sign of delbis
                if ((Math.Abs(delx)) < delmin)
                {
                    sgndel = delbis / (Math.Abs(delbis));
                    delx = 1.0000009 * sgndel * delmin;
                    delprv = delx;
                }
                // final check to insure that new x2 is in range of old x2 and x3
                // boundn is negative if new x2 is in range of old x2 and x3

                boundn = delx * (x2 + delx - x3);
                if (boundn > 0.0)
                {
                    // procedure stepping out of bounds, use bisection

                    delx = delbis;
                    delprv = delbis;
                }

                // relable variables for next iteration x1new=x2old, y1new=y2old

                x1 = x2;
                y1 = y2;

                // next iteration values for x2, y2

                x2 = x2 + delx;
                y2 = (pdetail(x2)) - p;

            } // for ()

            return x2;

        } // ddetail
        /****************************************************************************/

        public void calcAddnlGasConsts()
        {
            byte i, j;
            double xij, U1, K1;
            double eij, wij, e0p5, e2p0, e3p0, e3p5, e4p5, e6p0, e11p0, e7p5;
            double e9p5, e12p0, e12p5, s3;

            aga8_92.x[0] = gas_comp.CH4 / 100;       // CH4
            aga8_92.x[1] = gas_comp.N2 / 100;       // N2
            aga8_92.x[2] = gas_comp.CO2 / 100;       // CO2
            aga8_92.x[3] = gas_comp.C2H6 / 100;       // C2H6
            aga8_92.x[4] = gas_comp.C3H8 / 100;       // C3H8
            aga8_92.x[5] = gas_comp.H2O / 100;       // H2O
            aga8_92.x[6] = gas_comp.H2S / 100;       // H2S
            aga8_92.x[7] = gas_comp.H2 / 100;       // H2
            aga8_92.x[8] = gas_comp.CO / 100;       // CO
            aga8_92.x[9] = gas_comp.O2 / 100;       // O2
            aga8_92.x[10] = gas_comp.iC4H10 / 100;       // iC4H10
            aga8_92.x[11] = gas_comp.nC4H10 / 100;       // nC4H10
            aga8_92.x[12] = gas_comp.iC5H12 / 100;       // iC5H12
            aga8_92.x[13] = gas_comp.nC5H12 / 100;       // nC5H12
            aga8_92.x[14] = gas_comp.C6H14 / 100;       // C6H14
            aga8_92.x[15] = gas_comp.C7H16 / 100;       // C7H16
            aga8_92.x[16] = gas_comp.C8H18 / 100;       // C8H18
            aga8_92.x[17] = gas_comp.C9H20 / 100;       // C9H20
            aga8_92.x[18] = gas_comp.C10H22 / 100;       // C10H22
            aga8_92.x[19] = gas_comp.HE / 100;       // HE
            aga8_92.x[20] = gas_comp.AR / 100;       // AR

            for (i = 0; i < 21; i++)
                qi[i] = hi[i] = di[i] = mi[i] = 0.0;
            qi[2] = 0.690000;
            qi[5] = 1.067750;
            qi[6] = 0.633276;
            di[5] = hi[7] = 1.000000;
            mi[5] = 1.582200;
            mi[6] = 0.390000;

            // qi = Quadrupole Parameter
            // hi = High Temperature Parameter
            // di = Association Parameter
            // mi = Dipole Parameter

            // calculate F as per eqn 23, P 20, aga8 92
            aga8_92.F = aga8_92.x[7] * aga8_92.x[7]; 								// Hydrogen

            // calculate Q as per eqn 23, P 20, aga8 92
            aga8_92.Q = aga8_92.x[2] * 0.69000 +									// Carbon Dioxide
                        aga8_92.x[5] * 1.06775 + 									// Water
                        aga8_92.x[6] * 0.633276;									// Hydrogen Sulfide

            aga8_92.G = aga8_92.U = U1 = aga8_92.K = K1 = 0;

            for (i = 0; i < 18; i++)
                aga8_92.bTerm[i] = 0.0;

            for (i = 0; i < 21; i++)
            {
                if (aga8_92.x[i] <= 0.0)
                    continue;

                // first part of RHS of eq 21, P 20, aga8 92
                aga8_92.G += aga8_92.x[i] * charParams[i].G;

                // first part of RHS of eq 20, P 20, aga8 92
                U1 += (aga8_92.x[i]) * Math.Pow(charParams[i].E, 2.5);

                // first part of RHS of eq 14, P 18, aga8 92
                K1 += (aga8_92.x[i]) * Math.Pow(charParams[i].K, 2.5);

                for (j = i; j < 21; j++)
                {
                    if (aga8_92.x[j] <= 0.0)
                        continue;

                    xij = aga8_92.x[i] * aga8_92.x[j];
                    if (i != j)
                    {
                        xij *= 2.0;
                    }

                    // second part of RHS of eq 21, P 20, aga8 92
                    if (getGij(i, j) != 1.0)
                    {
                        aga8_92.G += xij * (getGij(i, j) - 1.0) *
                                     0.5 * (charParams[i].G + charParams[j].G);
                    }

                    // second part of RHS of eq 20, P 20, aga8 92
                    if (getUij(i, j) != 1.0)
                    {
                        aga8_92.U += xij * (Math.Pow(getUij(i, j), 5.0) - 1.0) *
                                           (Math.Pow(charParams[i].E *
                                                 charParams[j].E, 2.5));
                    }

                    // second part of RHS of eq 14, P 18, aga8 92
                    if (getKij(i, j) != 1.0)
                        aga8_92.K += xij * (Math.Pow(getKij(i, j), 5.0) - 1.0) *
                                           (Math.Pow(charParams[i].K * charParams[j].K, 2.5));

                    // terms in second virial coefficients
                    eij = getEij(i, j) * Math.Sqrt(charParams[i].E * charParams[j].E);
                    wij = getGij(i, j) * 0.5 * (charParams[i].G + charParams[j].G);
                    e0p5 = Math.Sqrt(eij);
                    e2p0 = eij * eij;
                    e3p0 = eij * e2p0;
                    e3p5 = e3p0 * e0p5;
                    e4p5 = eij * e3p5;
                    e6p0 = e3p0 * e3p0;
                    e11p0 = e4p5 * e4p5 * e2p0;
                    e7p5 = e4p5 * eij * e2p0;
                    e9p5 = e7p5 * e2p0;
                    e12p0 = e11p0 * eij;
                    e12p5 = e12p0 * e0p5;
                    s3 = xij * Math.Sqrt((Math.Pow(charParams[i].K, 3.0)) *
                                    (Math.Pow(charParams[j].K, 3.0)));
                    aga8_92.bTerm[0] += s3;
                    aga8_92.bTerm[1] += s3 * e0p5;
                    aga8_92.bTerm[2] += s3 * eij;
                    aga8_92.bTerm[3] += s3 * e3p5;
                    aga8_92.bTerm[4] += s3 * wij / e0p5;
                    aga8_92.bTerm[5] += s3 * wij * e4p5;
                    aga8_92.bTerm[6] += s3 * e0p5 * qi[i] * qi[j];
                    aga8_92.bTerm[7] += s3 * e7p5 * mi[i] * mi[j];
                    aga8_92.bTerm[8] += s3 * e9p5 * mi[i] * mi[j];
                    aga8_92.bTerm[9] += s3 * e6p0 * di[i] * di[j];
                    aga8_92.bTerm[10] += s3 * e12p0 * di[i] * di[j];
                    aga8_92.bTerm[11] += s3 * e12p5 * di[i] * di[j];
                    aga8_92.bTerm[12] += s3 / e6p0 * hi[i] * hi[j];
                    aga8_92.bTerm[13] += s3 * e2p0;
                    aga8_92.bTerm[14] += s3 * e3p0;
                    aga8_92.bTerm[15] += s3 * e2p0 * qi[i] * qi[j];
                    aga8_92.bTerm[16] += s3 * e2p0;
                    aga8_92.bTerm[17] += s3 * e11p0;
                } // for j

            } // for i

            // combine the two parts of U as per eq 20, P 20, aga8 92

            aga8_92.U = Math.Pow(aga8_92.U + U1 * U1, 0.20);
            aga8_92.K = Math.Pow(aga8_92.K + K1 * K1, 0.60); // K ^ 3

            for (i = 0; i < 18; i++)
                aga8_92.bTerm[i] *= stateParams[i].an;
        } // calcAddnlGasConsts

        public double calcMolarMass()
        {
            uint i;
            double Mrcalc;

            Mrcalc = 0;
            for (i = 0; i < 21; i++)
                Mrcalc += aga8_92.x[i] * Molar_mass[i];
            return (Mrcalc);
        }

        public double calcC0P(double T)
        {
            uint i;
            double c0p, aux;

            c0p = 0;
            T = T / 1.80;
            for (i = 0; i < 21; i++)
            {
                aux = hccoeff[i].B;
                if (hccoeff[i].D != 0)
                    aux += (hccoeff[i].C * Math.Pow(((hccoeff[i].D / T) / Math.Sinh(hccoeff[i].D / T)), 2));
                if (hccoeff[i].F != 0)
                    aux += (hccoeff[i].E * Math.Pow(((hccoeff[i].F / T) / Math.Cosh(hccoeff[i].F / T)), 2));
                if (hccoeff[i].H != 0)
                    aux += (hccoeff[i].G * Math.Pow(((hccoeff[i].H / T) / Math.Sinh(hccoeff[i].H / T)), 2));
                if (hccoeff[i].J != 0)
                    aux += (hccoeff[i].I * Math.Pow(((hccoeff[i].J / T) / Math.Cosh(hccoeff[i].J / T)), 2));
                aux *= (4.184 * aga8_92.x[i] / Mr);
                c0p += aux;
            }
            return (c0p);
        }

        public void calcdZdT(double d)
        {
            uint i;
            double s1, s2, D, T;

            s1 = s2 = 0;
            T = new_temp / 1.80;
            D = aga8_92.K * d;
            calcsn(d);
            for (i = 12; i < 58; i++)
            {
                s1 += stateParams[i].un * aga8_92.fn[i] * aga8_92.sn[i] / T;
                s2 += stateParams[i].un * (stateParams[i].un + 1) * aga8_92.fn[i] *
                      aga8_92.sn[i] / T / T;
            }

            dZdT = d * dBdT - s1;
            d2ZdT2 = d * d2BdT2 + s2;
        }

        public void calcdZdP(double T)
        {
            uint i;
            double D, s0, s1, s2, s3, bn, cn, kn, exp1;

            s0 = s1 = s2 = s3 = 0;
            T /= 1.80;
            D = aga8_92.K * density;

            for (i = 12; i < 58; i++)
            {
                bn = stateParams[i].bn;
                cn = stateParams[i].cn;
                kn = stateParams[i].kn;
                exp1 = Math.Exp(-cn * Math.Pow(D, kn));
                if (i < 18) s0 += aga8_92.fn[i];
                s1 += aga8_92.fn[i] * (-cn * kn * kn * Math.Pow(D, (kn - 1.0))) * Math.Pow(D, bn) * exp1;
                s2 += aga8_92.fn[i] * (bn - cn * kn * Math.Pow(D, kn)) * bn * Math.Pow(D, (bn - 1.0)) * exp1;
                s3 += aga8_92.fn[i] * (bn - cn * kn * Math.Pow(D, kn)) * Math.Pow(D, bn) * (cn * kn * Math.Pow(D, (kn - 1.0))) * exp1;
            }
            dZdP = aga8_92.K * ((coeff_B / aga8_92.K - s0) + s1 + s2 - s3);
        }

        public double calcCv()
        {
            uint i;
            double k3, k32, k33, k34, k35, k36, k37, k38, k39;
            double d, d2, d3, d4, d5, d6, d7, d8, d9;
            double D, D2, D3, D4, D5, D6, D7, exp1, exp2, exp3, exp4;
            double T, T2, cv0, cv1, cv2, cv3, cv4, cv5, cv6, cv7;

            T = new_temp / 1.80;
            T2 = T * T;
            k3 = aga8_92.K;
            k32 = k3 * k3;
            k33 = k32 * k3;
            k34 = k33 * k3;
            k35 = k34 * k3;
            k36 = k35 * k3;
            k37 = k36 * k3;
            k38 = k37 * k3;
            k39 = k38 * k3;

            d = density;
            d2 = d * d;
            d3 = d2 * d;
            d4 = d3 * d;
            d5 = d4 * d;
            d6 = d5 * d;
            d7 = d6 * d;
            d8 = d7 * d;
            d9 = d8 * d;

            D = k3 * d;
            D2 = D * D;
            D3 = D2 * D;
            D4 = D3 * D;
            D5 = D4 * D;
            D6 = D5 * D;
            D7 = D6 * D;

            exp1 = Math.Exp(-D);
            exp2 = Math.Exp(-D2);
            exp3 = Math.Exp(-D3);
            exp4 = Math.Exp(-D4);

            // Coeficientes para el calculo de la integral A

            A[12] = A[16] = A[17] = A[34] = A[35] = A[36] = 0;
            A[13] = A[14] = A[15] = rPI * (D + 0.02) / 2.0;
            A[18] = A[19] = D2;
            A[20] = A[21] = A[22] = 1.0 - exp2;
            A[23] = A[24] = A[25] = A[26] = rPI * (D2 + 0.02) / 2.0;
            A[27] = D3;
            A[28] = A[29] = -3.0 * k32 * (exp1 * (d2 + 2.0 * d / k3 + 2.0 / k32) - 2.0 / k32);
            A[30] = A[31] = 3.0 * k3 * (-d * exp2 / 2.0 + rPI * (D + 0.02) / 4.0 / k3);
            A[32] = A[33] = 1.0 - exp3;
            A[37] = A[38] = D4;
            A[39] = A[40] = A[41] = 2.0 * k32 * (1.0 / k32 - exp2 * (d2 + 1.0 / k32));
            A[42] = A[43] = 1.0 - exp4;
            A[44] = D5;
            A[45] = A[46] = 5.0 * k33 * (-exp2 * (d3 / 2.0 + 3.0 * d / 4.0 / k32) +
                            3.0 * rPI * (D + 0.02) / 8.0 / k33);
            A[47] = A[48] = -5.0 * k3 * d * exp4 / 4.0;
            A[49] = D6;
            A[50] = 3.0 * k34 * (-exp2 * (d4 + 2.0 * d2 / k32 + 2.0 / k34) + 2.0 / k34);
            A[51] = D7;
            A[52] = 7.0 * k35 * (-exp2 * (d5 / 2.0 + 5.0 * d3 / 4.0 / k32 + 15.0 * d / 8.0 / k34) +
                    15.0 * rPI * (D + 0.02) / 16.0 / k35);
            A[53] = (-8.0 * k37 * exp1 * (d7 + 7.0 * d6 / k3 + 42.0 * d5 / k32 + 210.0 * d4 / k33 +
                     840.0 * d3 / k34 + 2520.0 * d2 / k35 + 5040.0 * d / k36)) - 40320 * (exp1 - 1.0);
            A[54] = A[55] = (-8.0 * k36 * exp2 * (d6 / 2.0 + 3.0 * d4 / 2.0 / k32 + 3.0 * d2 / k34))
                    - 24.0 * (exp2 - 1.0);
            A[56] = A[57] = 9.0 * k37 * (exp2 * (-d7 / 2.0 - 7.0 * d5 / 4.0 / k32 - 35.0 * d3 / 8.0 / k34 -
                   105.0 * d / 16.0 / k36) + 105.0 * rPI * (D + 0.02) / 32.0 / k37);

            // Coeficientes para el calculo de la integral B

            B[12] = B[18] = B[19] = B[27] = B[37] = B[38] = B[44] = B[49] = B[51] = 0.0;
            B[13] = B[14] = B[15] = k3 * (-exp2 * d + rPI * (D + 0.02) / 2.0 / k3);
            B[16] = B[17] = -k3 * d * exp4;
            B[20] = B[21] = B[22] = 1.0 - (k32 * exp2 * (d2 + 1.0 / k32));
            B[23] = B[24] = B[25] = B[26] = k32 * (-exp4 * d2 + rPI * (D2 + 0.02) / 2.0 / k32);
            B[28] = B[29] = k33 * (-exp1 * (d3 + 3.0 * d2 / k3 + 6.0 * d / k32 + 6.0 / k33) + 6.0 / k33);
            B[30] = B[31] = k33 * (-exp2 * (d3 + 3.0 * d / 2.0 / k32) + 3.0 * rPI * (D + 0.02) / 4.0 / k33);
            B[32] = B[33] = k33 * (-exp3 * (d3 + 1.0 / k33) + 1.0 / k33);
            B[34] = B[35] = B[36] = -k33 * d3 * exp4;
            B[39] = B[40] = B[41] = -2.0 * k34 * (exp2 * (d4 / 2.0 + d2 / k32 + 1.0 / k34) - 1.0 / k34);
            B[42] = B[43] = k34 * (-exp4 * (d4 + 1.0 / k34) + 1.0 / k34);
            B[45] = B[46] = k35 * (-exp2 * (d5 + 5.0 * d3 / 2.0 / k32 + 15.0 * d / 4.0 / k34) + 15.0 * rPI *
                    (D + 0.02) / 8.0 / k35);
            B[47] = B[48] = k35 * (-exp4 * (d5 + 5.0 * d / 4.0 / k34));
            B[50] = 2.0 * k36 * (-exp2 * ((d6 / 2.0) + (3.0 * d4 / 2.0 / k32) + (3.0 * d2 / k34) +
                    (3.0 / k36))) + 6.0;
            B[52] = k37 * (-exp2 * (d7 + 7.0 * d5 / 2.0 / k32 + 35.0 * d3 / 4.0 / k34 + 105.0 * d / 8.0 / k36)) +
                    105.0 * rPI * (D + 0.02) / 16.0;
            B[53] = (-k38 * exp1 * (d8 + 8.0 * d7 / k3 + 56.0 * d6 / k32 + 336.0 * d5 / k33 + 1680.0 * d4 / k34 +
                    6720.0 * d3 / k35 + 20160.0 * d2 / k36 + 40320.0 * d / k37)) - 40320.0 * (exp1 - 1.0);
            B[54] = B[55] = (-k38 * exp2 * (d8 + 4.0 * d6 / k32 + 12.0 * d4 / k34 + 24.0 * d2 / k36)) - 24.0 * (exp2 - 1.0);
            B[56] = B[57] = k39 * (-exp2 * (d9 + 9.0 * d7 / 2.0 / k32 + 63.0 * d5 / 4.0 / k34 + 315.0 *
                    d3 / 8.0 / k36 + 945.0 * d / 16.0 / k38) + 945.0 * rPI * (D + 0.02) / 32.0 / k39);

            cv0 = d * dBdT;
            cv1 = d * d2BdT2;
            cv2 = cv3 = cv4 = cv5 = 0.0;
            for (i = 12; i < 58; i++)
            {
                if (i < 18)
                {
                    cv2 += D * stateParams[i].un * (stateParams[i].un + 1.0) * aga8_92.fn[i] / T2;
                    cv3 += D * stateParams[i].un * aga8_92.fn[i] / T;
                }
                cv4 += stateParams[i].un * (stateParams[i].un + 1.0) * aga8_92.fn[i] * (A[i] - B[i]) / T2;
                cv5 += stateParams[i].un * aga8_92.fn[i] * (A[i] - B[i]) / T;
            }
            cv6 = T2 * (cv1 - cv2 + cv4);
            cv7 = 2.0 * T * (cv0 + cv3 - cv5);
            return (c0p - (8.31451 / Mr) * (1.0 + cv6 + cv7));
        }

        public void calcfn(double T)
        {
            double tr, tr0p5, tr1p5, tr2p0, tr3p0, tr4p0, tr5p0, tr6p0, tr7p0;
            double tr8p0, tr9p0, tr11p0, tr13p0, tr21p0, tr22p0, tr23p0;

            tr = T / (aga8_92.U);
            tr0p5 = Math.Sqrt(tr);
            tr1p5 = tr * tr0p5;
            tr2p0 = tr * tr;
            tr3p0 = tr * tr2p0;
            tr4p0 = tr * tr3p0;
            tr5p0 = tr * tr4p0;
            tr6p0 = tr * tr5p0;
            tr7p0 = tr * tr6p0;
            tr8p0 = tr * tr7p0;
            tr9p0 = tr * tr8p0;
            tr11p0 = tr6p0 * tr5p0;
            tr13p0 = tr6p0 * tr7p0;
            tr21p0 = tr9p0 * tr9p0 * tr3p0;
            tr22p0 = tr * tr21p0;
            tr23p0 = tr * tr22p0;

            aga8_92.fn[12] = stateParams[12].an * aga8_92.F * tr6p0;
            aga8_92.fn[13] = stateParams[13].an / tr2p0;
            aga8_92.fn[14] = stateParams[14].an / tr3p0;
            aga8_92.fn[15] = stateParams[15].an * aga8_92.Q * aga8_92.Q / tr2p0;
            aga8_92.fn[16] = stateParams[16].an / tr2p0;
            aga8_92.fn[17] = stateParams[17].an / tr11p0;
            aga8_92.fn[18] = stateParams[18].an * tr0p5;
            aga8_92.fn[19] = stateParams[19].an / tr0p5;
            aga8_92.fn[20] = stateParams[20].an;
            aga8_92.fn[21] = stateParams[21].an / tr4p0;
            aga8_92.fn[22] = stateParams[22].an / tr6p0;
            aga8_92.fn[23] = stateParams[23].an / tr21p0;
            aga8_92.fn[24] = stateParams[24].an * aga8_92.G / tr23p0;
            aga8_92.fn[25] = stateParams[25].an * aga8_92.Q * aga8_92.Q / tr22p0;
            aga8_92.fn[26] = stateParams[26].an * aga8_92.F * tr;
            aga8_92.fn[27] = stateParams[27].an * aga8_92.Q * aga8_92.Q * tr0p5;
            aga8_92.fn[28] = stateParams[28].an * aga8_92.G / tr7p0;
            aga8_92.fn[29] = stateParams[29].an * aga8_92.F * tr;
            aga8_92.fn[30] = stateParams[30].an / tr6p0;
            aga8_92.fn[31] = stateParams[31].an * aga8_92.G / tr4p0;
            aga8_92.fn[32] = stateParams[32].an * aga8_92.G / tr;
            aga8_92.fn[33] = stateParams[33].an * aga8_92.G / tr9p0;
            aga8_92.fn[34] = stateParams[34].an * aga8_92.F * tr13p0;
            aga8_92.fn[35] = stateParams[35].an / tr21p0;
            aga8_92.fn[36] = stateParams[36].an * aga8_92.Q * aga8_92.Q / tr8p0;
            aga8_92.fn[37] = stateParams[37].an * tr0p5;
            aga8_92.fn[38] = stateParams[38].an;
            aga8_92.fn[39] = stateParams[39].an / tr2p0;
            aga8_92.fn[40] = stateParams[40].an / tr7p0;
            aga8_92.fn[41] = stateParams[41].an * aga8_92.Q * aga8_92.Q / tr9p0;
            aga8_92.fn[42] = stateParams[42].an / tr22p0;
            aga8_92.fn[43] = stateParams[43].an / tr23p0;
            aga8_92.fn[44] = stateParams[44].an / tr;
            aga8_92.fn[45] = stateParams[45].an / tr9p0;
            aga8_92.fn[46] = stateParams[46].an * aga8_92.Q * aga8_92.Q / tr3p0;
            aga8_92.fn[47] = stateParams[47].an / tr8p0;
            aga8_92.fn[48] = stateParams[48].an * aga8_92.Q * aga8_92.Q / tr23p0;
            aga8_92.fn[49] = stateParams[49].an / tr1p5;
            aga8_92.fn[50] = stateParams[50].an * aga8_92.G / tr5p0;
            aga8_92.fn[51] = stateParams[51].an * aga8_92.Q * aga8_92.Q * tr0p5;
            aga8_92.fn[52] = stateParams[52].an / tr4p0;
            aga8_92.fn[53] = stateParams[53].an * aga8_92.G / tr7p0;
            aga8_92.fn[54] = stateParams[54].an / tr3p0;
            aga8_92.fn[55] = stateParams[55].an * aga8_92.G;
            aga8_92.fn[56] = stateParams[56].an / tr;
            aga8_92.fn[57] = stateParams[57].an * aga8_92.Q * aga8_92.Q;
        }

        public void calcsn(double d)
        {
            double d1, d2, d3, d4, d5, d6, d7, d8, d9, exp1, exp2, exp3, exp4;

            d1 = aga8_92.K * d; /* d = molar density */
            d2 = d1 * d1;
            d3 = d2 * d1;
            d4 = d3 * d1;
            d5 = d4 * d1;
            d6 = d5 * d1;
            d7 = d6 * d1;
            d8 = d7 * d1;
            d9 = d8 * d1;

            exp1 = Math.Exp(-d1);
            exp2 = Math.Exp(-d2);
            exp3 = Math.Exp(-d3);
            exp4 = Math.Exp(-d4);

            aga8_92.sn[12] = d1 * (exp3 - 1.0 - 3.0 * d3 * exp3);
            aga8_92.sn[13] = aga8_92.sn[14] = aga8_92.sn[15] = d1 * (exp2 - 1.0 - 2.0 * d2 * exp2);
            aga8_92.sn[16] = aga8_92.sn[17] = d1 * (exp4 - 1.0 - 4.0 * d4 * exp4);
            aga8_92.sn[18] = aga8_92.sn[19] = d2 * 2.0;
            aga8_92.sn[20] = aga8_92.sn[21] = aga8_92.sn[22] = d2 * (2.0 - 2.0 * d2) * exp2;
            aga8_92.sn[23] = aga8_92.sn[24] = aga8_92.sn[25] = d2 * (2.0 - 4.0 * d4) * exp4;
            aga8_92.sn[26] = d2 * (2.0 - 4.0 * d4) * exp4;
            aga8_92.sn[27] = d3 * 3.0;
            aga8_92.sn[28] = aga8_92.sn[29] = d3 * (3.0 - d1) * exp1;
            aga8_92.sn[30] = aga8_92.sn[31] = d3 * (3.0 - 2.0 * d2) * exp2;
            aga8_92.sn[32] = aga8_92.sn[33] = d3 * (3.0 - 3.0 * d3) * exp3;
            aga8_92.sn[34] = aga8_92.sn[35] = aga8_92.sn[36] = d3 * (3.0 - 4.0 * d4) * exp4;
            aga8_92.sn[37] = aga8_92.sn[38] = d4 * 4.0;
            aga8_92.sn[39] = aga8_92.sn[40] = aga8_92.sn[41] = d4 * (4.0 - 2.0 * d2) * exp2;
            aga8_92.sn[42] = aga8_92.sn[43] = d4 * (4.0 - 4.0 * d4) * exp4;
            aga8_92.sn[44] = d5 * 5.0;
            aga8_92.sn[45] = aga8_92.sn[46] = d5 * (5.0 - 2.0 * d2) * exp2;
            aga8_92.sn[47] = aga8_92.sn[48] = d5 * (5.0 - 4.0 * d4) * exp4;
            aga8_92.sn[49] = d6 * 6.0;
            aga8_92.sn[50] = d6 * (6.0 - 2.0 * d2) * exp2;
            aga8_92.sn[51] = d7 * 7.0;
            aga8_92.sn[52] = d7 * (7.0 - 2.0 * d2) * exp2;
            aga8_92.sn[53] = d8 * (8.0 - d1) * exp1;
            aga8_92.sn[54] = aga8_92.sn[55] = d8 * (8.0 - 2.0 * d2) * exp2;
            aga8_92.sn[56] = aga8_92.sn[57] = d9 * (9.0 - 2.0 * d2) * exp2;
        }

        public double calcVoS(double Temp, double Pres)
        {
            // 1 - Input the contract base temperature (Tb), contract base
            //     pressure (Tb), the operating temperature (Tf), operating
            //     pressure (Tf) and gas analysis.

            Tb = 59.0;
            Pb = 14.69271022;
            Tf = (Temp * 1.8) + 32.0;		// Temperatura en °F
            Pf = Pres * 14.22334;        // Presión en psia

            // 2 - Calculate the molar mass of the mixture

            calcAddnlGasConsts();
            Mr = calcMolarMass();

            // 3 - Calculate the compressibility and density of the fluid at the
            //     conditions of interest

            new_temp = Tf + 459.67;
            new_prsr = Pf;
            aga8_table(ZF);

            // 4 - Calculate the ideal gas constant pressure heat capacity at
            //     at the operating temperature

            c0p = calcC0P(new_temp);

            // 5 - Calculate the real gas constant volume heat capacity at the
            //     operating conditions

            calcdZdT(density);
            calcdZdP(new_temp);
            cv = calcCv();

            // 6 - Calculate the real gas constant pressure heat capacity at
            //     the operating conditions

            cp = cv + 8.31451 / Mr * Math.Pow((Zf + (new_temp / 1.80) * dZdT), 2.0) /
                 (Zf + density * dZdP);

            // 7 - Calculate the ratio of capacities, Cp/Cv, at the operating
            //     conditions

            Cr = cp / cv;

            // Calculate the speed of sound, based on the results of the preceding steps */

            return (Math.Sqrt(Cr * (RGAS * 1e6 * new_temp / 1.80 / Mr) * (Zf + density * dZdP)));
        }
    
    }

}
