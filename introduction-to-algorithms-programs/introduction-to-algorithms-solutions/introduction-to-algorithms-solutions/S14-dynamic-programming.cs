namespace introduction_to_algorithms_solutions;

class S14_dynamic_programming
{
    // ----------------------------------------------------------- Rod cutting -----------------------------------------
    public int rod_cutting_recurrence(int[] prices, int rod_length)
    {
        if (rod_length == 0) return 0;
        int calculated = Int32.MinValue;
        
        for (int i = 1; i <= rod_length; i++)
        {
            calculated = Math.Max(calculated, prices[i] + rod_cutting_recurrence(prices, rod_length - i));
        }

        return calculated;
    }

    public int rod_cutting_from_top(int[] prices, int rod_length)
    {
        int rod_cutting_internal(int[] prices, int rod_l, int[] calculated)
        {
            if (calculated[rod_l] >= 0) return calculated[rod_l];

            int current_lowest = 0;
            for (int i = 1; i <= rod_l; i++)
            {
                current_lowest = Math.Max(current_lowest, prices[i-1] + rod_cutting_internal(prices, rod_l - i, calculated));
            }
            
            return calculated[rod_l] = current_lowest;
        }
        
        var calculated = new int[rod_length + 1];
        for (int i = 0; i <= rod_length; i++)
        {
            calculated[i] = Int32.MinValue;
        }

        return rod_cutting_internal(prices, rod_length, calculated);
    }

    public int rod_cutting_from_bottom_up(int[] prices, int rod_length)
    {
        int[] calculated = new int[rod_length + 1];
        for (int i = 0; i <= rod_length; i++)
        {
            calculated[i] = Int32.MinValue;
        }

        calculated[0] = 0;
        for (int i = 1; i <= rod_length; i++)
        {
            for (int j = 1; j <= i; j++)
            {
                calculated[i] = Math.Max(calculated[i], prices[j-1] + calculated[i - j]);
            }
        }

        return calculated[rod_length];
    }
    // ----------------------------------------------------------- Rod cutting end -------------------------------------

}