namespace RecruitmentPlatform.API.Services
{
    public class AiMatchingService
    {
        public double CalculateMatchScore(string candidateSkills, string jobRequiredSkills)
        {
            List<string> candidateSkillList = NormalizeSkills(candidateSkills);
            List<string> jobSkillList = NormalizeSkills(jobRequiredSkills);

            if (!jobSkillList.Any())
                return 0;

            int matchedCount = jobSkillList.Count(jobSkill =>
                candidateSkillList.Any(candidateSkill =>
                    candidateSkill == jobSkill ||
                    candidateSkill.Contains(jobSkill) ||
                    jobSkill.Contains(candidateSkill)));

            double score = (double)matchedCount / jobSkillList.Count * 100;

            return Math.Round(score, 2);
        }

        private static List<string> NormalizeSkills(string skills)
        {
            if (string.IsNullOrWhiteSpace(skills))
                return new List<string>();

            return skills
                .ToLower()
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(skill => skill.Trim())
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Distinct()
                .ToList();
        }
    }
}