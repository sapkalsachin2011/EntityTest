public class Solution {
    public string LongestPalindrome(string s) {
        if (string.IsNullOrEmpty(s)) return "";

        int start = 0, maxLen = 1;
        for (int i = 0; i < s.Length; i++) {
            // Odd length palindrome
            ExpandAroundCenter(s, i, i, ref start, ref maxLen);
            // Even length palindrome
            ExpandAroundCenter(s, i, i + 1, ref start, ref maxLen);
        }
        return s.Substring(start, maxLen);
    }

    private void ExpandAroundCenter(string s, int left, int right, ref int start, ref int maxLen) {
        while (left >= 0 && right < s.Length && s[left] == s[right]) {
            if (right - left + 1 > maxLen) {
                start = left;
                maxLen = right - left + 1;
            }
            left--;
            right++;
        }
    }
}
