import React from 'react';
import DefaultAdmonitionTypes from '@theme-original/Admonition/Types';

function PraiseAdmonition(props) {
  return (
    <div class="theme-admonition alert alert--praise">
      <h5>{props.title}</h5>
      <div>{props.children}</div>
    </div>
  );
}

const AdmonitionTypes = {
  ...DefaultAdmonitionTypes,

  // Add all your custom admonition types here...
  // You can also override the default ones if you want
  'praise': PraiseAdmonition,
};

export default AdmonitionTypes;