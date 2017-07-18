using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QLNet.DateGeneration;

namespace QLNet.Termstructures.Credit
{

   public class DefaultProbabilityHelper : BootstrapHelper<DefaultProbabilityTermStructure>
   {
      public DefaultProbabilityHelper() : base() { } // required for generics
      public DefaultProbabilityHelper(Handle<Quote> quote) : base(quote) { }
      public DefaultProbabilityHelper(double quote) : base(quote) { }

   }

   public class RelativeDateDefaultProbabilityHelper : RelativeDateBootstrapHelper<DefaultProbabilityTermStructure>
   {
      public RelativeDateDefaultProbabilityHelper() : base() { } // required for generics
      public RelativeDateDefaultProbabilityHelper(Handle<Quote> quote) : base(quote) { }
      public RelativeDateDefaultProbabilityHelper(double quote) : base(quote) { }
   }

   public class CdsHelper : RelativeDateDefaultProbabilityHelper
   {

      protected Period tenor_;
      protected int settlementDays_;
      protected Calendar calendar_;
      protected Frequency frequency_;
      protected BusinessDayConvention paymentConvention_;
      protected Rule rule_;
      protected DayCounter dayCounter_;
      protected double recoveryRate_;
      protected Handle<YieldTermStructure> discountCurve_;
      protected bool settlesAccrual_;
      protected bool paysAtDefaultTime_;

      protected Schedule schedule_;
      protected CreditDefaultSwap swap_;
      protected RelinkableHandle<DefaultProbabilityTermStructure> probability_;
      //! protection effective date.
      protected Date protectionStart_;

      public CdsHelper(Handle<Quote> quote,
                   Period tenor,
                   int settlementDays,
                   Calendar calendar,
                   Frequency frequency,
                   BusinessDayConvention paymentConvention,
                   Rule rule,
                   DayCounter dayCounter,
                   double recoveryRate,
                   Handle<YieldTermStructure> discountCurve,
                   bool settlesAccrual = true,
                   bool paysAtDefaultTime = true) : base(quote)
      {

         tenor_ = tenor;
         settlementDays_ = settlementDays;
         calendar_ = calendar;
         frequency_ = frequency;
         paymentConvention_ = paymentConvention;
         rule_ = rule;
         dayCounter_ = dayCounter;
         recoveryRate_ = recoveryRate;
         discountCurve_ = discountCurve;
         settlesAccrual_ = settlesAccrual;
         paysAtDefaultTime_ = paysAtDefaultTime;

         initializeDates();
         discountCurve_.registerWith(update);
      }

      public CdsHelper(double quote,
                  Period tenor,
                  int settlementDays,
                  Calendar calendar,
                  Frequency frequency,
                  BusinessDayConvention paymentConvention,
                  Rule rule,
                  DayCounter dayCounter,
                  double recoveryRate,
                  Handle<YieldTermStructure> discountCurve,
                  bool settlesAccrual = true,
                  bool paysAtDefaultTime = true) : base(quote)
      {
         tenor_ = tenor;
         settlementDays_ = settlementDays;
         calendar_ = calendar;
         frequency_ = frequency;
         paymentConvention_ = paymentConvention;
         rule_ = rule;
         dayCounter_ = dayCounter;
         recoveryRate_ = recoveryRate;
         discountCurve_ = discountCurve;
         settlesAccrual_ = settlesAccrual;
         paysAtDefaultTime_ = paysAtDefaultTime;

         initializeDates();
         discountCurve_.registerWith(update);
      }

      public override void setTermStructure(DefaultProbabilityTermStructure ts)
      {
         base.setTermStructure(ts);
         probability_ = new RelinkableHandle<DefaultProbabilityTermStructure>();
         probability_.linkTo(ts);
         resetEngine();
      }

      public override void update()
      {
         base.update();
         resetEngine();
      }
      protected override void initializeDates()
      {
         protectionStart_ = evaluationDate_ + settlementDays_;
         Date startDate = calendar_.adjust(protectionStart_, paymentConvention_);
         Date endDate = evaluationDate_ + tenor_;

         schedule_ = new MakeSchedule().from(startDate)
                          .to(endDate)
                          .withFrequency(frequency_)
                          .withCalendar(calendar_)
                          .withConvention(paymentConvention_)
                          .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
                          .withRule(rule_).value();

         earliestDate_ = schedule_.dates().FirstOrDefault();
         latestDate_ = calendar_.adjust(schedule_.dates().LastOrDefault(), paymentConvention_);
      }
      protected virtual void resetEngine() { }
   }

   //! Spread-quoted CDS hazard rate bootstrap helper.
   public class SpreadCdsHelper : CdsHelper
   {
      public SpreadCdsHelper(Handle<Quote> runningSpread,
                        Period tenor,
                        int settlementDays,
                        Calendar calendar,
                        Frequency frequency,
                        BusinessDayConvention paymentConvention,
                        Rule rule,
                        DayCounter dayCounter,
                        double recoveryRate,
                        Handle<YieldTermStructure> discountCurve,
                        bool settlesAccrual = true,
                        bool paysAtDefaultTime = true) : base(runningSpread, tenor, settlementDays, calendar, frequency, paymentConvention, rule, dayCounter, recoveryRate, discountCurve, settlesAccrual, paysAtDefaultTime)
      {
      }

      public SpreadCdsHelper(double runningSpread,
                        Period tenor,
                        int settlementDays,
                        Calendar calendar,
                        Frequency frequency,
                        BusinessDayConvention paymentConvention,
                        Rule rule,
                        DayCounter dayCounter,
                        double recoveryRate,
                        Handle<YieldTermStructure> discountCurve,
                        bool settlesAccrual = true,
                        bool paysAtDefaultTime = true) : base(runningSpread, tenor, settlementDays, calendar, frequency, paymentConvention, rule, dayCounter, recoveryRate, discountCurve, settlesAccrual, paysAtDefaultTime)
      {
      }

      public override double impliedQuote()
      {
         swap_.recalculate();
         return swap_.fairSpread();
      }

      protected override void resetEngine()
      {
         swap_ = new CreditDefaultSwap(Protection.Side.Buyer, 100.0, 0.01,
                                         schedule_, paymentConvention_,
                                         dayCounter_, settlesAccrual_,
                                         paysAtDefaultTime_,
                                         protectionStart_);

         swap_.setPricingEngine(new MidPointCdsEngine(probability_,
                                                      recoveryRate_,
                                                      discountCurve_));
      }
   }

   //! Upfront-quoted CDS hazard rate bootstrap helper.
   public class UpfrontCdsHelper : CdsHelper
   {
      private int upfrontSettlementDays_;
      Date upfrontDate_;
      double runningSpread_;

      /*! \note the upfront must be quoted in fractional units. */
      UpfrontCdsHelper(Handle<Quote> upfront,
                         double runningSpread,
                         Period tenor,
                         int settlementDays,
                         Calendar calendar,
                         Frequency frequency,
                         BusinessDayConvention paymentConvention,
                         Rule rule,
                         DayCounter dayCounter,
                         double recoveryRate,
                         Handle<YieldTermStructure> discountCurve,
                         int upfrontSettlementDays = 0,
                         bool settlesAccrual = true,
                         bool paysAtDefaultTime = true) : base(upfront, tenor, settlementDays, calendar, frequency, paymentConvention, rule, dayCounter, recoveryRate, discountCurve, settlesAccrual, paysAtDefaultTime)
      {
         upfrontSettlementDays_ = upfrontSettlementDays;
         runningSpread_ = runningSpread;

         initializeDates();
      }

      /*! \note the upfront must be quoted in fractional units. */
      UpfrontCdsHelper(double upfront,
                       double runningSpread,
                          Period tenor,
                         int settlementDays,
                         Calendar calendar,
                         Frequency frequency,
                         BusinessDayConvention paymentConvention,
                         Rule rule,
                         DayCounter dayCounter,
                         double recoveryRate,
                         Handle<YieldTermStructure> discountCurve,
                         int upfrontSettlementDays = 0,
                         bool settlesAccrual = true,
                         bool paysAtDefaultTime = true) : base(runningSpread, tenor, settlementDays, calendar, frequency, paymentConvention, rule, dayCounter, recoveryRate, discountCurve, settlesAccrual, paysAtDefaultTime)
      {
         upfrontSettlementDays_ = upfrontSettlementDays;
         runningSpread_ = runningSpread;

         initializeDates();
      }

      public override double impliedQuote()
      {
         // Testing forward performance option greeks
         SavedSettings backup = new SavedSettings();

         Settings.includeTodaysCashFlows = true;
         swap_.recalculate();
         return swap_.fairUpfront();
      }

      protected override void initializeDates()
      {
         base.initializeDates();
         upfrontDate_ = calendar_.advance(evaluationDate_,
                                         upfrontSettlementDays_, TimeUnit.Days,
                                         paymentConvention_);
      }
      protected override void resetEngine()
      {
         swap_ = new CreditDefaultSwap(Protection.Side.Buyer, 100.0,
                                               0.01, runningSpread_,
                                               schedule_, paymentConvention_,
                                               dayCounter_,
                                               settlesAccrual_,
                                               paysAtDefaultTime_,
                                               protectionStart_,
                                               upfrontDate_);

         swap_.setPricingEngine(new MidPointCdsEngine(probability_,
                                                             recoveryRate_,
                                                             discountCurve_,
                                                             true));
      }
   }

}
