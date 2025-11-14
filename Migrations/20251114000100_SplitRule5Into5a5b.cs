using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderPhotography.Migrations
{
    public partial class SplitRule5Into5a5b : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Shift existing rule IDs >= 6 up by 1 to make room for new id 6
            migrationBuilder.Sql(@"
                UPDATE ""PhotoSubmissions""
                SET ""CompositionRuleId"" = ""CompositionRuleId"" + 1
                WHERE ""CompositionRuleId"" >= 6
            ");

            // Normalize CompositionRuleName to the new rule names
            migrationBuilder.Sql(@"
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Rule of Thirds' WHERE ""CompositionRuleId"" = 1;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Leading Lines' WHERE ""CompositionRuleId"" = 2;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Framing Natural' WHERE ""CompositionRuleId"" = 3;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Fill the Frame' WHERE ""CompositionRuleId"" = 4;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Symmetry (5a)' WHERE ""CompositionRuleId"" = 5;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Asymmetry (5b)' WHERE ""CompositionRuleId"" = 6;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Patterns & Repetition' WHERE ""CompositionRuleId"" = 7;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Golden Ratio' WHERE ""CompositionRuleId"" = 8;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Diagonals' WHERE ""CompositionRuleId"" = 9;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Center Dominant Eye' WHERE ""CompositionRuleId"" = 10;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Picture to Ground' WHERE ""CompositionRuleId"" = 11;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert CompositionRuleName back to original combined names
            migrationBuilder.Sql(@"
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Rule of Thirds' WHERE ""CompositionRuleId"" = 1;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Leading Lines' WHERE ""CompositionRuleId"" = 2;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Framing Natural' WHERE ""CompositionRuleId"" = 3;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Fill the Frame' WHERE ""CompositionRuleId"" = 4;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Symmetry & Asymmetry' WHERE ""CompositionRuleId"" = 5;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Patterns & Repetition' WHERE ""CompositionRuleId"" = 6;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Golden Ratio' WHERE ""CompositionRuleId"" = 7;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Diagonals' WHERE ""CompositionRuleId"" = 8;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Center Dominant Eye' WHERE ""CompositionRuleId"" = 9;
                UPDATE ""PhotoSubmissions"" SET ""CompositionRuleName"" = 'Picture to Ground' WHERE ""CompositionRuleId"" = 10;
            ");

            // Shift IDs >=7 down by 1 to restore original numbering
            migrationBuilder.Sql(@"
                UPDATE ""PhotoSubmissions""
                SET ""CompositionRuleId"" = ""CompositionRuleId"" - 1
                WHERE ""CompositionRuleId"" >= 7
            ");
        }
    }
}
