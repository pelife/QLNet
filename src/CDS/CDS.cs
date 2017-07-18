/*
 Copyright (C) 2008 Jose Aparicio
 Copyright (C) 2014 Felipe Almeida(felipe.almeida@gmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)  
 * 
 This file is part of QLNet Project http://qlnet.sourceforge.net/

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://qlnet.sourceforge.net/License.html>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QLNet;
using QLNet.Termstructures.Credit;

namespace CDS
{
   class CDS
   {
      static void Main(string[] args)
      {

         Calendar calendar = new TARGET();
         Date todaysDate = new Date(15, Month.May, 2007);
         // must be a business day
         todaysDate = calendar.adjust(todaysDate);


         // nothing to do with Date::todaysDate
         Settings.setEvaluationDate(todaysDate);

         // dummy curve
         double flatQuote = 0.01;
         Quote flatQuoteRate = new SimpleQuote(flatQuote);
         FlatForward flatForward = new FlatForward(todaysDate, flatQuoteRate, new Actual365Fixed());
         Handle<YieldTermStructure> tsCurve = new Handle<YieldTermStructure>(flatForward);

         /*
          In Lehmans Brothers "guide to exotic credit derivatives"
          p. 32 there's a simple case, zero flat curve with a flat CDS
          curve with constant market spreads of 150 bp and RR = 50%
          corresponds to a flat 3% hazard rate. The implied 1-year
          survival probability is 97.04% and the 2-years is 94.18%
         */
         double recovery_rate = 0.5;
         double[] quoted_spreads = { 0.0150, 0.0150, 0.0150, 0.0150 };

         List<Period> tenors = new List<Period>();
         tenors.Add(new Period(3, TimeUnit.Months));
         tenors.Add(new Period(6, TimeUnit.Months));
         tenors.Add(new Period(1, TimeUnit.Years));
         tenors.Add(new Period(2, TimeUnit.Years));

         List<Date> maturities = new List<Date>();
         for (int i = 0; i < tenors.Count; i++)
         {
            maturities.Add(calendar.adjust(todaysDate + tenors[i], BusinessDayConvention.Following));
         }

         List<RelativeDateDefaultProbabilityHelper> instruments = new List<RelativeDateDefaultProbabilityHelper>();

         for (int i = 0; i < maturities.Count; i++)
         {
            instruments.Add(new SpreadCdsHelper(new Handle<Quote>(new SimpleQuote(quoted_spreads[i])),
               tenors[i],
               0,
               calendar,
               Frequency.Quarterly,
               BusinessDayConvention.Following,
               DateGeneration.Rule.TwentiethIMM,
               new Actual365Fixed(),
               recovery_rate,
               tsCurve));

         }

         
      }
   }
}
