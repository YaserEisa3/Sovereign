namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 8. What each spending line IS. Same job as the tax explanations: what the
    /// money buys, who notices, and how long it takes to show up.
    /// </summary>
    public static partial class ProjectBuilder
    {
        static string SpendExplanation(string key)
        {
            switch (key)
            {
                case "Medicaid":
                    return "Health cover for the poorest households. The Poor score you on it directly, and it is "
                         + "one of the two lines that decide how badly a pandemic hurts.";
                case "Medicare":
                    return "Health cover for the retired. It grows on its own as the population ages, whether you "
                         + "vote for it or not.";
                case "PublicHealth":
                    return "Clinics, surveillance, vaccination. The pandemic mitigant: its level AT THE MOMENT a "
                         + "pathogen arrives is what decides the damage, and raising it afterwards is too late.";
                case "SocialSecurityRetirement":
                    return "State pensions. The largest single promise you have made, and it grows with the retired "
                         + "band every year.";
                case "SocialSecurityDisability":
                    return "Support for people who cannot work. Small, steady, and politically untouchable.";
                case "UnemploymentInsurance":
                    return "Payments to people out of work. It rises by itself in a recession - your deficit worsens "
                         + "whether you act or not, which is the point of a stabiliser.";
                case "HousingAssistance":
                    return "Help with rent. The Middle judge you on housing costs, and this is the lever that "
                         + "touches them without waiting for the market.";
                case "FoodAssistance":
                    return "Food support for low-income households. Cheap, fast, and the first thing the Poor feel "
                         + "when it is cut.";
                case "DisasterReliefFund":
                    return "A balance rather than a flow: it absorbs disaster damage and depletes. Underfunded, the "
                         + "shortfall lands on your infrastructure instead.";
                case "EducationK12":
                    return "Schools. The Middle score you on it, and its real payoff - a better workforce - arrives "
                         + "long after this government has gone.";
                case "EducationHigher":
                    return "Universities and colleges. Feeds the Technology sector with people, slowly.";
                case "InfraRoads":
                case "InfraTransit":
                case "InfraEnergyGrid":
                case "InfraBroadband":
                case "InfraWater":
                case "InfraAirports":
                    return "One of the six infrastructure lines. Together they hold the infrastructure gauge up; "
                         + "below 70 it drags on everything the economy produces, and repairs cost more the longer "
                         + "they wait.";
                case "ScienceRnD":
                    return "Research funding. A five-year lag before it shows in growth - most runs never see the "
                         + "payoff, and that is honest.";
                case "SpaceProgram":
                    return "Prestige, and a slow feed into Technology. Nobody votes for it and everybody remembers it.";
                case "AgriculturalSubsidies":
                    return "Payments to farmers. Holds the Agriculture sector's health up and food prices down.";
                case "EnergySubsidies":
                    return "Support for energy producers or bills. It softens an oil shock at the till, and costs "
                         + "you every month it runs.";
                case "DebtService":
                    return "Interest on the national debt. You do not set this - the bond market does, through the "
                         + "yields you have earned. It is the first claim on every dollar you raise.";
                case "MilitaryPersonnel":
                    return "Soldiers' pay. The Right score you on defence, and a war you are not manned for goes "
                         + "worse than one you are.";
                case "MilitaryEquipment":
                    return "Kit and its replacement. It is what a war consumes, and what a war leaves you short of.";
                case "MilitaryRnD":
                    return "Defence research. Slow, expensive, and it spills into the civilian Technology sector.";
                case "MilitaryOperations":
                    return "The cost of actually doing things: deployments, fuel, flying hours. It is what a war "
                         + "bills you weekly.";
                case "HomelandSecurity":
                    return "Domestic security. It cushions unrest at the edges without addressing the cause.";
                case "VeteransBenefits":
                    return "What you owe the people you already sent. Computed from the veteran population rather "
                         + "than chosen - a war raises this line for decades.";
                case "Intelligence":
                    return "Agencies and analysis. Quietly improves your warning time on other countries' moves.";
                case "ForeignAid":
                    return "Money to other nations. It buys relationships, and it is what decides whether Emerging "
                         + "South's restructuring ends as a thank-you or a default.";
                default:
                    return "";
            }
        }
    }
}
