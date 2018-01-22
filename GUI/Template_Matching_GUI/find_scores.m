function match_score = find_scores( spike_array,sample1,sample2, templates)
%could more efficiently implement this using repmat to make 2 x 3d matrices
%and then subtracting and doing abs & sum on the correct dimension

n_cats = size(templates,1);
match_score =[];

if isempty(spike_array) || isempty(templates)
    return;
end

match_score = zeros(n_cats, size(spike_array,1));

for i = 1:n_cats
    for j=1: size(spike_array,1)
        match_score(i,j) = sum(abs(double(spike_array(j,sample1:sample2)) - templates(i,sample1:sample2)));
        
    end
end

